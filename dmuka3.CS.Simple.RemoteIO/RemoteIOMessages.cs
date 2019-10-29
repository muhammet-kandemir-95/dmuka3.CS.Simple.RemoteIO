using System;

namespace dmuka3.CS.Simple.RemoteIO
{
	/// <summary>
	/// Protocol messages.
	/// </summary>
    internal static class RemoteIOMessages
	{
		#region Variables
		internal const string SERVER_HI = "HI";
		internal const string SERVER_NOT_AUTHORIZED = "NOT_AUTHORIZED";
		internal const string SERVER_OK = "OK";
		internal const string SERVER_NOT_FOUND = "NOT_FOUND";
		internal const string SERVER_FOUND = "FOUND";
		internal const string SERVER_END = "END";
		internal const string SERVER_ERROR = "ERROR";

		internal const string CLIENT_HI = "HI";
		internal const string CLIENT_READ_FILE = "READ_FILE";
		internal const string CLIENT_WRITE_FILE = "WRITE_FILE";
		internal const string CLIENT_DELETE_FILE = "DELETE_FILE";
		internal const string CLIENT_CREATE_DIRECTORY = "CREATE_DIRECTORY";
		internal const string CLIENT_DELETE_DIRECTORY = "DELETE_DIRECTORY";
		internal const string CLIENT_FILE_EXISTS = "FILE_EXISTS";
		internal const string CLIENT_DIRECTORY_EXISTS = "DIRECTORY_EXISTS";
		internal const string CLIENT_GET_FILES = "GET_FILES";
		internal const string CLIENT_GET_FILE_SIZE = "GET_FILE_SIZE";
		internal const string CLIENT_CLOSE = "CLOSE";
		#endregion
	}
}
