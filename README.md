# dmuka3.CS.Simple.RemoteIO

 This library provides you to connect to files and directories. Thus, you can manage your file system using network.
 
 ## Nuget
 **Link** : https://www.nuget.org/packages/dmuka3.CS.Simple.RemoteIO
 ```nuget
 Install-Package dmuka3.CS.Simple.RemoteIO
 ```
 
 ## Example 1

  We should create a server to connect to the file system. After that, we will try something on RemoteIO server.
  
```csharp
// We are creating a server.
RemoteIOServer server = new RemoteIOServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
      // Start server async.
new Thread(() =>
{
  server.Start();
}).Start();

      // Wait the server
Thread.Sleep(1000);

      // Create a client to connect to server.
RemoteIOClient client = new RemoteIOClient("127.0.0.1", 9090);
      // Start communication.
client.Start("muhammed", "123123", 2048);

      // Write a file with byte[].
client.WriteFile("my_nunit_text_file.txt", Encoding.UTF8.GetBytes("MUHAMMET KANDEMİR"));
      // Read a file from server.
var v = Encoding.UTF8.GetString(client.ReadFile("my_nunit_text_file.txt"));
      // Delete a file on server.
client.DeleteFile("my_nunit_text_file.txt");

      // Delete a directory recursive on server.
client.DeleteDirectory("my_nunit_directory");
      // Create a directory on server.
client.CreateDirectory("my_nunit_directory");
      // Create a directory on server again.
      client.CreateDirectory("my_nunit_directory\\sub");
      // Checking file exists.
var exists1 = client.FileExists("my_nunit_directory\\sub\\test.txt");
      // Write a file with byte[].
client.WriteFile("my_nunit_directory\\sub\\test.txt", Encoding.UTF8.GetBytes("MUHAMMET KANDEMİR"));
      // Checking file exists.
      var exists2 = client.FileExists("my_nunit_directory\\sub\\test.txt");
      // Write a file with byte[].
      client.WriteFile("my_nunit_directory\\test.txt", Encoding.UTF8.GetBytes("MUHAMMET KANDEMİR"));
      // Get file list in a directory from server.
var files = client.GetFiles("my_nunit_directory", onlyCurrent: false);
      // Delete a directory recursive.
client.DeleteDirectory("my_nunit_directory");

      // Close client and server.
client.Dispose();
server.Dispose();

Assert.IsFalse(exists1);
Assert.IsTrue(exists2);
Assert.AreEqual(v, "MUHAMMET KANDEMİR");
Assert.AreEqual(files.Length, 2);
Assert.AreEqual(files[0], "test.txt");
Assert.AreEqual(files[1], "sub\\test.txt");  
```

 You may wonder what is the "2048". It is key size of RSA. We always use RSA during communication on TCP. This is security to save your secret datas.
 
 You have to know that you will never get an error while you are using some methods. For instance, DeleteDirectory doesn't throw and error if the directory doesn't exist. Just like DeleteFile. But don't forget, you can get an error about another reasons(File locked, path is wrong or something like that).
