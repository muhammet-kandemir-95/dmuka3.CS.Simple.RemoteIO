using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using dmuka3.CS.Simple.Semaphore;
using dmuka3.CS.Simple.TCP;

namespace dmuka3.CS.Simple.RemoteIO
{

	/// <summary>
	/// Server to read/write files and to create/delete directories.
	/// </summary>
	public class RemoteIOServer : IDisposable
	{
		#region Variables
		/// <summary>
		/// Server port.
		/// </summary>
		public int Port { get; private set; }

		/// <summary>
		/// Time out auth as second.
		/// </summary>
		public int TimeOutAuth { get; private set; }

		/// <summary>
		/// Server authentication user name.
		/// </summary>
		public string UserName { get; private set; }

		/// <summary>
		/// Server authentication password.
		/// </summary>
		public string Password { get; private set; }

		/// <summary>
		/// Maximum connection count at the same time processing.
		/// </summary>
		public int CoreCount { get; private set; }

		/// <summary>
		/// RSA key size as bit.
		/// </summary>
		public int RSADwKeySize { get; private set; }

		/// <summary>
		/// Server.
		/// </summary>
		private TcpListener _listener = null;

		/// <summary>
		/// Manage the semaphore for connections.
		/// </summary>
		private ActionQueue _actionQueueConnections = null;
		#endregion

		#region Constructors
		/// <summary>
		/// Instance server.
		/// </summary>
		/// <param name="userName">Server authentication user name.</param>
		/// <param name="password">Server authentication password.</param>
		/// <param name="rsaDwKeySize">SSL key size as bit.</param>
		/// <param name="coreCount">Maximum connection count at the same time processing.</param>
		/// <param name="port">Server port.</param>
		/// <param name="timeOutAuth">Time out auth as second.</param>
		public RemoteIOServer(string userName, string password, int rsaDwKeySize, int coreCount, int port, int timeOutAuth = 1)
		{
			this.UserName = userName;
			this.Password = password;
			this.TimeOutAuth = timeOutAuth;
			this.RSADwKeySize = rsaDwKeySize;
			this._listener = new TcpListener(IPAddress.Any, port);
			this._actionQueueConnections = new ActionQueue(coreCount);
		}
		#endregion

		#region Methods
		/// <summary>
		/// Start the server as sync.
		/// </summary>
		public void Start()
		{
			this._actionQueueConnections.Start();
			this._listener.Start();

			while (true)
			{
				TcpClient client = null;

				try
				{
					client = this._listener.AcceptTcpClient();
				}
				catch
				{
					break;
				}

				this._actionQueueConnections.AddAction(() =>
				{
					try
					{
						var conn = new TCPClientConnection(client);
						// SERVER : HI
						conn.Send(
							Encoding.UTF8.GetBytes(
								RemoteIOMessages.SERVER_HI
								));

						conn.StartDMUKA3RSA(this.RSADwKeySize);

						// CLIENT : HI <user_name> <password>
						var clientHi = Encoding.UTF8.GetString(
											conn.Receive(timeOutSecond: this.TimeOutAuth)
											);
						var splitClientHi = clientHi.Split('<');
						var clientHiUserName = splitClientHi[1].Split('>')[0];
						var clientHiPassword = splitClientHi[2].Split('>')[0];
						if (this.UserName != clientHiUserName || this.Password != clientHiPassword)
						{
							// - IF AUTH FAIL
							//      SERVER : NOT_AUTHORIZED
							conn.Send(
								Encoding.UTF8.GetBytes(
									RemoteIOMessages.SERVER_NOT_AUTHORIZED
									));
							conn.Dispose();
						}
						else
						{
							// - IF AUTH PASS
							//      SERVER : OK
							conn.Send(
								Encoding.UTF8.GetBytes(
									RemoteIOMessages.SERVER_OK
									));

							while (true)
							{
								var clientProcess = Encoding.UTF8.GetString(
														conn.Receive()
														);

								#region READ_FILE
								if (clientProcess.StartsWith(RemoteIOMessages.CLIENT_READ_FILE))
								{
									// - IF PROCESS TYPE IS "READ FILE BY PATH"
									//      CLIENT : READ_FILE <path>
									string path = "";
									try
									{
										path = clientProcess.Split('<')[1].Split('>')[0];
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_READ_FILE)}.GetPath> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									try
									{
										if (File.Exists(path))
										{
											conn.Send(
												Encoding.UTF8.GetBytes(
													RemoteIOMessages.SERVER_FOUND
													));
											//      SERVER : file_byte[]
											conn.Send(
												File.ReadAllBytes(
													path
												));
										}
										else
										{
											conn.Send(
												Encoding.UTF8.GetBytes(
													RemoteIOMessages.SERVER_NOT_FOUND
													));
										}
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_READ_FILE)}.GetFile> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									//      SERVER : END
									conn.Send(
										Encoding.UTF8.GetBytes(
											RemoteIOMessages.SERVER_END
											));
								}
								#endregion
								#region WRITE_FILE
								else if (clientProcess.StartsWith(RemoteIOMessages.CLIENT_WRITE_FILE))
								{
									// - IF PROCESS TYPE IS "WRITE FILE BY PATH"
									//      CLIENT : WRITE_FILE <path>
									string path = "";
									try
									{
										path = clientProcess.Split('<')[1].Split('>')[0];
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_WRITE_FILE)}.GetPath> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									//      CLIENT : file_byte[]
									try
									{
										var fileByteArray = conn.Receive();
										File.WriteAllBytes(path, fileByteArray);
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_WRITE_FILE)}.WriteFile> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									//      SERVER : END
									conn.Send(
										Encoding.UTF8.GetBytes(
											RemoteIOMessages.SERVER_END
											));
								}
								#endregion
								#region CREATE_DIRECTORY
								else if (clientProcess.StartsWith(RemoteIOMessages.CLIENT_CREATE_DIRECTORY))
								{
									// - IF PROCESS TYPE IS "CREATE DIRECTORY BY PATH"
									//      CLIENT : CREATE_DIRECTORY <path>
									string path = "";
									try
									{
										path = clientProcess.Split('<')[1].Split('>')[0];
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_CREATE_DIRECTORY)}.GetPath> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									try
									{
										Directory.CreateDirectory(path);
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_CREATE_DIRECTORY)}.CreateDirectory> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									//      SERVER : END
									conn.Send(
										Encoding.UTF8.GetBytes(
											RemoteIOMessages.SERVER_END
											));
								}
								#endregion
								#region DELETE_DIRECTORY
								else if (clientProcess.StartsWith(RemoteIOMessages.CLIENT_DELETE_DIRECTORY))
								{
									// - IF PROCESS TYPE IS "DELETE DIRECTORY BY PATH"
									//      CLIENT : DELETE_DIRECTORY <path>
									string path = "";
									try
									{
										path = clientProcess.Split('<')[1].Split('>')[0];
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_DELETE_DIRECTORY)}.GetPath> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									try
									{
										if (Directory.Exists(path))
											Directory.Delete(path, true);
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_DELETE_DIRECTORY)}.DeleteDirectory> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									//      SERVER : END
									conn.Send(
										Encoding.UTF8.GetBytes(
											RemoteIOMessages.SERVER_END
											));
								}
								#endregion
								#region DELETE_FILE
								else if (clientProcess.StartsWith(RemoteIOMessages.CLIENT_DELETE_FILE))
								{
									// - IF PROCESS TYPE IS "DELETE FILE BY PATH"
									//      CLIENT : DELETE_FILE <path>
									string path = "";
									try
									{
										path = clientProcess.Split('<')[1].Split('>')[0];
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_DELETE_FILE)}.GetPath> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									try
									{
										if (File.Exists(path))
											File.Delete(path);
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_DELETE_FILE)}.DeleteFile> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									//      SERVER : END
									conn.Send(
										Encoding.UTF8.GetBytes(
											RemoteIOMessages.SERVER_END
											));
								}
								#endregion
								#region FILE_EXISTS
								else if (clientProcess.StartsWith(RemoteIOMessages.CLIENT_FILE_EXISTS))
								{
									// - IF PROCESS TYPE IS "FILE EXISTS BY PATH"
									//      CLIENT : FILE_EXISTS <path>
									string path = "";
									try
									{
										path = clientProcess.Split('<')[1].Split('>')[0];
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_FILE_EXISTS)}.GetPath> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									if (File.Exists(path))
									{
										// - IF FILE EXISTS
										//		SERVER: FOUND
										conn.Send(
											Encoding.UTF8.GetBytes(
												RemoteIOMessages.SERVER_FOUND
												));
									}
									else
									{
										// - IF FILE NOT EXISTS
										//		SERVER: NOT_FOUND
										conn.Send(
											Encoding.UTF8.GetBytes(
												RemoteIOMessages.SERVER_NOT_FOUND
												));
									}

									//      SERVER : END
									conn.Send(
										Encoding.UTF8.GetBytes(
											RemoteIOMessages.SERVER_END
											));
								}
								#endregion
								#region DIRECTORY_EXISTS
								else if (clientProcess.StartsWith(RemoteIOMessages.CLIENT_DIRECTORY_EXISTS))
								{
									// - IF PROCESS TYPE IS "DIRECTORY EXISTS BY PATH"
									//      CLIENT : DIRECTORY_EXISTS <path>
									string path = "";
									try
									{
										path = clientProcess.Split('<')[1].Split('>')[0];
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_DIRECTORY_EXISTS)}.GetPath> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									if (Directory.Exists(path))
									{
										// - IF DIRECTORY EXISTS
										//		SERVER: FOUND
										conn.Send(
											Encoding.UTF8.GetBytes(
												RemoteIOMessages.SERVER_FOUND
												));
									}
									else
									{
										// - IF DIRECTORY NOT EXISTS
										//		SERVER: NOT_FOUND
										conn.Send(
											Encoding.UTF8.GetBytes(
												RemoteIOMessages.SERVER_NOT_FOUND
												));
									}

									//      SERVER : END
									conn.Send(
										Encoding.UTF8.GetBytes(
											RemoteIOMessages.SERVER_END
											));
								}
								#endregion
								#region GET_FILES
								else if (clientProcess.StartsWith(RemoteIOMessages.CLIENT_GET_FILES))
								{
									// - IF PROCESS TYPE IS "DIRECTORY EXISTS BY PATH"
									//      CLIENT : GET_FILES <path> <search_pattern> <current/with_sub>
									string[] splitClientProcess = clientProcess.Split('<');
									string path = "";
									try
									{
										path = splitClientProcess[1].Split('>')[0];
										path = Path.GetFullPath(path);
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_GET_FILES)}.GetPath> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}
									string searchPattern = "";
									try
									{
										searchPattern = splitClientProcess[2].Split('>')[0];
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_GET_FILES)}.GetSearchPattern> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}
									string currentOrWithSub = "";
									try
									{
										currentOrWithSub = splitClientProcess[3].Split('>')[0].ToLowerInvariant();
										if (currentOrWithSub != "current" && currentOrWithSub != "with_sub")
											throw new Exception("Wrong enum value! ('current' or 'with_sub')");
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_GET_FILES)}.GetCurrentOrWithSub> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									var skipDirectory = path.Length;
									if (!path.EndsWith("" + Path.DirectorySeparatorChar)) skipDirectory++;

									string[] files = null;
									try
									{
										if (Directory.Exists(path) == false)
											files = new string[0];
										else if (searchPattern == "" && currentOrWithSub == "current")
											files = Directory.GetFiles(path);
										else if (searchPattern != "" && currentOrWithSub == "current")
											files = Directory.GetFiles(path, searchPattern);
										else
											files = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);
										files = files.Select(o => o.Substring(skipDirectory)).ToArray();
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_GET_FILES)}.GetList> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									//      SERVER: json_array_data
									conn.Send(
										Encoding.UTF8.GetBytes(
											JsonConvert.SerializeObject(
												files
												)));

									//      SERVER : END
									conn.Send(
										Encoding.UTF8.GetBytes(
											RemoteIOMessages.SERVER_END
											));
								}
								#endregion
								#region GET_FILE_SIZE
								else if (clientProcess.StartsWith(RemoteIOMessages.CLIENT_GET_FILE_SIZE))
								{
									// - IF PROCESS TYPE IS "GET FILE SIZE BY PATH"
									//      CLIENT : GET_FILE_SIZE <path>
									string path = "";
									try
									{
										path = clientProcess.Split('<')[1].Split('>')[0];
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_GET_FILE_SIZE)}.GetPath> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									//      SERVER : len
									try
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												new FileInfo(path).Length.ToString(CultureInfo.InvariantCulture)
												));
									}
									catch (Exception ex)
									{
										conn.Send(
											Encoding.UTF8.GetBytes(
												$"{RemoteIOMessages.SERVER_ERROR} <{nameof(RemoteIOMessages.CLIENT_GET_FILE_SIZE)}.GetFileInfo> \"{ex.ToString().Replace("\"", "\\\"")}\""
												));
										continue;
									}

									//      SERVER : END
									conn.Send(
										Encoding.UTF8.GetBytes(
											RemoteIOMessages.SERVER_END
											));
								}
								#endregion
								#region CLOSE
								else if (clientProcess.StartsWith(RemoteIOMessages.CLIENT_CLOSE))
								{
									// - IF PROCESS TYPE IS "CLOSE"
									//      CLIENT : CLOSE
									//      SERVER : END
									conn.Dispose();
									break;
								}
								#endregion
							}
						}
					}
					catch (Exception ex)
					{
						try
						{
							client.Dispose();
						}
						catch
						{ }

						Console.WriteLine("A connetion get an error = " + ex.ToString());
					}
				});
			}
		}

		/// <summary>
		/// Dispose.
		/// </summary>
		public void Dispose()
		{
			this._actionQueueConnections.Dispose();
			this._listener.Stop();
		}
		#endregion
	}
}
