using dmuka3.CS.Simple.TCP;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace dmuka3.CS.Simple.RemoteIO
{
	/// <summary>
	/// Client for reading and setting datas.
	/// </summary>
	public class RemoteIOClient : IDisposable
	{
		#region Variables
		private object lockObj = new object();

		/// <summary>
		/// Wrong protocol exception.
		/// </summary>
		private static Exception __wrongProtocolException = new Exception("Wrong protocol!");

		/// <summary>
		/// Server's host name.
		/// </summary>
		public string HostName { get; set; }

		/// <summary>
		/// Server's port.
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// TCP client.
		/// </summary>
		private TcpClient _client = null;

		/// <summary>
		/// For dmuka protocol.
		/// </summary>
		private TCPClientConnection _conn = null;
		#endregion

		#region Constructors
		/// <summary>
		/// Create an instance.
		/// </summary>
		/// <param name="hostName">Server's host name.</param>
		/// <param name="port">Server's port.</param>
		public RemoteIOClient(string hostName, int port)
		{
			this.HostName = hostName;
			this.Port = port;

			this._client = new TcpClient();
		}
		#endregion

		#region Methods
		/// <summary>
		/// Connect the server.
		/// </summary>
		/// <param name="userName">Server authentication user name.</param>
		/// <param name="password">Server authentication password.</param>
		/// <param name="dwKeySize">SSL key size as bit.</param>
		public void Start(string userName, string password, int dwKeySize)
		{
			if (userName.Contains('<') || userName.Contains('>') || password.Contains('<') || password.Contains('>'))
				throw new Exception("UserName and Password can't containt '<' or '>'!");

			this._client.Connect(this.HostName, this.Port);
			this._conn = new TCPClientConnection(this._client);

			// SERVER : HI
			var serverHi = Encoding.UTF8.GetString(
								this._conn.Receive()
								);

			if (serverHi == RemoteIOMessages.SERVER_HI)
			{
				this._conn.StartDMUKA3RSA(dwKeySize);

				// CLIENT : HI <user_name> <password>
				this._conn.Send(
					Encoding.UTF8.GetBytes(
						$"{RemoteIOMessages.CLIENT_HI} <{userName}> <{password}>"
						));

				var serverResAuth = Encoding.UTF8.GetString(
										this._conn.Receive()
										);

				if (serverResAuth == RemoteIOMessages.SERVER_NOT_AUTHORIZED)
					// - IF AUTH FAIL
					//      SERVER : NOT_AUTHORIZED
					throw new Exception($"{RemoteIOMessages.SERVER_NOT_AUTHORIZED} - Not authorized!");
				else if (serverResAuth != RemoteIOMessages.SERVER_OK)
					// - IF AUTH PASS
					//      SERVER : OK
					throw __wrongProtocolException;

			}
			else
				throw __wrongProtocolException;
		}

		#region Manage Datas
		/// <summary>
		/// Read a file from server.
		/// </summary>
		/// <param name="path">File path.</param>
		/// <returns></returns>
		public byte[] ReadFile(string path)
		{
			if (path.Contains('<') || path.Contains('>'))
				throw new Exception("Path can't contain '<' or '>'!");

			lock (lockObj)
			{
				// - IF PROCESS TYPE IS "READ FILE BY PATH"
				//      CLIENT : READ_FILE <path>
				this._conn.Send(
					Encoding.UTF8.GetBytes(
						$"{RemoteIOMessages.CLIENT_READ_FILE} <{path}>"
						));

				var serverResFound = Encoding.UTF8.GetString(
										this._conn.Receive()
										);
				if (serverResFound == RemoteIOMessages.SERVER_NOT_FOUND)
				{
					// - IF FILE NOT EXISTS
					//      SERVER : NOT_FOUND
					//      SERVER : END
					var serverResEnd = Encoding.UTF8.GetString(
											this._conn.Receive()
											);

					if (serverResEnd == RemoteIOMessages.SERVER_END)
						return null;
					else
						throw __wrongProtocolException;
				}
				else if (serverResFound == RemoteIOMessages.SERVER_FOUND)
				{
					// - IF FILE EXISTS
					//      SERVER : FOUND
					//      SERVER : data
					var data = this._conn.Receive();

					//      SERVER : END
					var serverResEnd = Encoding.UTF8.GetString(
											this._conn.Receive()
											);

					if (serverResEnd == RemoteIOMessages.SERVER_END)
						return data;
					else
						throw __wrongProtocolException;
				}
				else if (serverResFound.StartsWith(RemoteIOMessages.SERVER_ERROR))
					throw new Exception(serverResFound);
				else
					throw __wrongProtocolException;
			}
		}

		/// <summary>
		/// Write a file to server.
		/// </summary>
		/// <param name="path">File path.</param>
		/// <param name="file">File's bytes.</param>
		/// <returns></returns>
		public void WriteFile(string path, byte[] file)
		{
			if (path.Contains('<') || path.Contains('>'))
				throw new Exception("Path can't contain '<' or '>'!");

			lock (lockObj)
			{
				// - IF PROCESS TYPE IS "WRITE FILE WITH PATH"
				//      CLIENT : WRITE_FILE <path>
				this._conn.Send(
					Encoding.UTF8.GetBytes(
						$"{RemoteIOMessages.CLIENT_WRITE_FILE} <{path}>"
						));

				//      CLIENT : file_byte[]
				this._conn.Send(file);

				//      SERVER : END
				var serverResEnd = Encoding.UTF8.GetString(
										this._conn.Receive()
										);
				if (serverResEnd == RemoteIOMessages.SERVER_END)
					return;
				else if (serverResEnd.StartsWith(RemoteIOMessages.SERVER_ERROR))
					throw new Exception(serverResEnd);
				else
					throw __wrongProtocolException;
			}
		}

		/// <summary>
		/// Create a directory on server.
		/// </summary>
		/// <param name="path">Directory path.</param>
		/// <returns></returns>
		public void CreateDirectory(string path)
		{
			if (path.Contains('<') || path.Contains('>'))
				throw new Exception("Path can't contain '<' or '>'!");

			lock (lockObj)
			{
				// - IF PROCESS TYPE IS "CREATE DIRECTORY WITH PATH"
				//      CLIENT : CREATE_DIRECTORY <path>
				this._conn.Send(
					Encoding.UTF8.GetBytes(
						$"{RemoteIOMessages.CLIENT_CREATE_DIRECTORY} <{path}>"
						));

				//      SERVER : END
				var serverResEnd = Encoding.UTF8.GetString(
										this._conn.Receive()
										);
				if (serverResEnd == RemoteIOMessages.SERVER_END)
					return;
				else if (serverResEnd.StartsWith(RemoteIOMessages.SERVER_ERROR))
					throw new Exception(serverResEnd);
				else
					throw __wrongProtocolException;
			}
		}

		/// <summary>
		/// Delete a directory on server.
		/// </summary>
		/// <param name="path">Directory path.</param>
		/// <returns></returns>
		public void DeleteDirectory(string path)
		{
			if (path.Contains('<') || path.Contains('>'))
				throw new Exception("Path can't contain '<' or '>'!");

			lock (lockObj)
			{
				// - IF PROCESS TYPE IS "DELETE DIRECTORY WITH PATH"
				//      CLIENT : DELETE_DIRECTORY <path>
				this._conn.Send(
					Encoding.UTF8.GetBytes(
						$"{RemoteIOMessages.CLIENT_DELETE_DIRECTORY} <{path}>"
						));

				//      SERVER : END
				var serverResEnd = Encoding.UTF8.GetString(
										this._conn.Receive()
										);
				if (serverResEnd == RemoteIOMessages.SERVER_END)
					return;
				else if (serverResEnd.StartsWith(RemoteIOMessages.SERVER_ERROR))
					throw new Exception(serverResEnd);
				else
					throw __wrongProtocolException;
			}
		}

		/// <summary>
		/// Delete a file on server.
		/// </summary>
		/// <param name="path">File path.</param>
		/// <returns></returns>
		public void DeleteFile(string path)
		{
			if (path.Contains('<') || path.Contains('>'))
				throw new Exception("Path can't contain '<' or '>'!");

			lock (lockObj)
			{
				// - IF PROCESS TYPE IS "DELETE FILE WITH PATH"
				//      CLIENT : DELETE_FILE <path>
				this._conn.Send(
					Encoding.UTF8.GetBytes(
						$"{RemoteIOMessages.CLIENT_DELETE_FILE} <{path}>"
						));

				//      SERVER : END
				var serverResEnd = Encoding.UTF8.GetString(
										this._conn.Receive()
										);
				if (serverResEnd == RemoteIOMessages.SERVER_END)
					return;
				else if (serverResEnd.StartsWith(RemoteIOMessages.SERVER_ERROR))
					throw new Exception(serverResEnd);
				else
					throw __wrongProtocolException;
			}
		}

		/// <summary>
		/// Check to exists a file on server.
		/// </summary>
		/// <param name="path">File path.</param>
		/// <returns></returns>
		public bool FileExists(string path)
		{
			if (path.Contains('<') || path.Contains('>'))
				throw new Exception("Path can't contain '<' or '>'!");

			lock (lockObj)
			{
				// - IF PROCESS TYPE IS "FILE EXISTS BY PATH"
				//      CLIENT : FILE_EXISTS <path>
				this._conn.Send(
					Encoding.UTF8.GetBytes(
						$"{RemoteIOMessages.CLIENT_FILE_EXISTS} <{path}>"
						));

				var serverResFound = Encoding.UTF8.GetString(
										this._conn.Receive()
										);
				if (serverResFound == RemoteIOMessages.SERVER_NOT_FOUND)
				{
					// - IF FILE NOT EXISTS
					//      SERVER : NOT_FOUND
					//      SERVER : END
					var serverResEnd = Encoding.UTF8.GetString(
											this._conn.Receive()
											);

					if (serverResEnd == RemoteIOMessages.SERVER_END)
						return false;
					else
						throw __wrongProtocolException;
				}
				else if (serverResFound == RemoteIOMessages.SERVER_FOUND)
				{
					// - IF FILE EXISTS
					//      SERVER : FOUND
					//      SERVER : END
					var serverResEnd = Encoding.UTF8.GetString(
											this._conn.Receive()
											);

					if (serverResEnd == RemoteIOMessages.SERVER_END)
						return true;
					else
						throw __wrongProtocolException;
				}
				else if (serverResFound.StartsWith(RemoteIOMessages.SERVER_ERROR))
					throw new Exception(serverResFound);
				else
					throw __wrongProtocolException;
			}
		}

		/// <summary>
		/// Check to exists a directory on server.
		/// </summary>
		/// <param name="path">Directory path.</param>
		/// <returns></returns>
		public bool DirectoryExists(string path)
		{
			if (path.Contains('<') || path.Contains('>'))
				throw new Exception("Path can't contain '<' or '>'!");

			lock (lockObj)
			{
				// - IF PROCESS TYPE IS "FILE EXISTS BY PATH"
				//      CLIENT : FILE_EXISTS <path>
				this._conn.Send(
					Encoding.UTF8.GetBytes(
						$"{RemoteIOMessages.CLIENT_DIRECTORY_EXISTS} <{path}>"
						));

				var serverResFound = Encoding.UTF8.GetString(
										this._conn.Receive()
										);
				if (serverResFound == RemoteIOMessages.SERVER_NOT_FOUND)
				{
					// - IF DIRECTORY NOT EXISTS
					//      SERVER : NOT_FOUND
					//      SERVER : END
					var serverResEnd = Encoding.UTF8.GetString(
											this._conn.Receive()
											);

					if (serverResEnd == RemoteIOMessages.SERVER_END)
						return false;
					else
						throw __wrongProtocolException;
				}
				else if (serverResFound == RemoteIOMessages.SERVER_FOUND)
				{
					// - IF DIRECTORY EXISTS
					//      SERVER : FOUND
					//      SERVER : END
					var serverResEnd = Encoding.UTF8.GetString(
											this._conn.Receive()
											);

					if (serverResEnd == RemoteIOMessages.SERVER_END)
						return true;
					else
						throw __wrongProtocolException;
				}
				else if (serverResFound.StartsWith(RemoteIOMessages.SERVER_ERROR))
					throw new Exception(serverResFound);
				else
					throw __wrongProtocolException;
			}
		}

		/// <summary>
		/// Get file's list by directory from server.
		/// </summary>
		/// <param name="path">Directory path.</param>
		/// <param name="searchPattern">Search pattern.</param>
		/// <param name="onlyCurrent">If this value is false, the server will find with all sub directories in current directory.</param>
		/// <returns></returns>
		public string[] GetFiles(string path, string searchPattern = "", bool onlyCurrent = true)
		{
			if (path.Contains('<') || path.Contains('>'))
				throw new Exception("Path can't contain '<' or '>'!");

			if (searchPattern.Contains('<') || searchPattern.Contains('>'))
				throw new Exception("Search Pattern can't contain '<' or '>'!");

			lock (lockObj)
			{
				// - IF PROCESS TYPE IS "GET FILES BY PATH"
				//      CLIENT : GET_FILES <path> <search_pattern> <current/with_sub>
				this._conn.Send(
					Encoding.UTF8.GetBytes(
						$"{RemoteIOMessages.CLIENT_GET_FILES} <{path}> <{searchPattern}> <{(onlyCurrent == true ? "current" : "with_sub")}>"
						));

				//      SERVER : json_array_data
				var serverResFiles = Encoding.UTF8.GetString(
										this._conn.Receive()
										);
				if (serverResFiles.StartsWith(RemoteIOMessages.SERVER_ERROR))
					throw new Exception(serverResFiles);

				//      SERVER : END
				var serverResEnd = Encoding.UTF8.GetString(
										this._conn.Receive()
										);
				if (serverResEnd == RemoteIOMessages.SERVER_END)
					return JsonConvert.DeserializeObject<string[]>(serverResFiles);
				else if (serverResEnd.StartsWith(RemoteIOMessages.SERVER_ERROR))
					throw new Exception(serverResEnd);
				else
					throw __wrongProtocolException;
			}
		}

		/// <summary>
		/// Get file's size on server.
		/// </summary>
		/// <param name="path">File path.</param>
		/// <returns></returns>
		public long GetFiles(string path)
		{
			if (path.Contains('<') || path.Contains('>'))
				throw new Exception("Path can't contain '<' or '>'!");

			lock (lockObj)
			{
				// - IF PROCESS TYPE IS "GET FILES BY PATH"
				//      CLIENT : GET_FILES <path> <search_pattern> <current/with_sub>
				this._conn.Send(
					Encoding.UTF8.GetBytes(
						$"{RemoteIOMessages.CLIENT_GET_FILE_SIZE} <{path}>"
						));

				//      SERVER : json_array_data
				var serverResFileSize = Encoding.UTF8.GetString(
										this._conn.Receive()
										);
				if (serverResFileSize.StartsWith(RemoteIOMessages.SERVER_ERROR))
					throw new Exception(serverResFileSize);

				//      SERVER : END
				var serverResEnd = Encoding.UTF8.GetString(
										this._conn.Receive()
										);
				if (serverResEnd == RemoteIOMessages.SERVER_END)
					return Convert.ToInt64(serverResFileSize, CultureInfo.InvariantCulture);
				else if (serverResEnd.StartsWith(RemoteIOMessages.SERVER_ERROR))
					throw new Exception(serverResEnd);
				else
					throw __wrongProtocolException;
			}
		}
		#endregion

		/// <summary>
		/// Dispose.
		/// </summary>
		public void Dispose()
		{
			try
			{
				this._conn.Send(
					Encoding.UTF8.GetBytes(
						$"{RemoteIOMessages.CLIENT_CLOSE}"
						));
			}
			catch
			{ }
			this._conn.Dispose();
		}
		#endregion
	}
}