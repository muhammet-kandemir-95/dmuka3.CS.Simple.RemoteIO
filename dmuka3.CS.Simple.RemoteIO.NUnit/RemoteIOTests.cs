using System;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace dmuka3.CS.Simple.RemoteIO.NUnit
{
	public class RemoteIOTests
	{
		[Test]
		public void ClassicTest()
		{
			RemoteIOServer server = new RemoteIOServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
			new Thread(() =>
			{
				server.Start();
			}).Start();

			Thread.Sleep(1000);

			RemoteIOClient client = new RemoteIOClient("127.0.0.1", 9090);
			client.Start("muhammed", "123123", 2048);

			client.WriteFile("my_nunit_text_file.txt", Encoding.UTF8.GetBytes("MUHAMMET KANDEMİR"));
			var v = Encoding.UTF8.GetString(client.ReadFile("my_nunit_text_file.txt"));
			client.DeleteFile("my_nunit_text_file.txt");

			client.DeleteDirectory("my_nunit_directory");
			client.CreateDirectory("my_nunit_directory");
			client.CreateDirectory("my_nunit_directory\\sub");
			var exists1 = client.FileExists("my_nunit_directory\\sub\\test.txt");
			client.WriteFile("my_nunit_directory\\sub\\test.txt", Encoding.UTF8.GetBytes("MUHAMMET KANDEMİR"));
			var exists2 = client.FileExists("my_nunit_directory\\sub\\test.txt");
			client.WriteFile("my_nunit_directory\\test.txt", Encoding.UTF8.GetBytes("MUHAMMET KANDEMİR"));
			var files = client.GetFiles("my_nunit_directory", onlyCurrent: false);
			client.DeleteDirectory("my_nunit_directory");

			client.Dispose();
			server.Dispose();

			Assert.IsFalse(exists1);
			Assert.IsTrue(exists2);
			Assert.AreEqual(v, "MUHAMMET KANDEMİR");
			Assert.AreEqual(files.Length, 2);
			Assert.AreEqual(files[0], "test.txt");
			Assert.AreEqual(files[1], "sub\\test.txt");
		}

		[Test]
		public void WrongUserNameTest()
		{
			RemoteIOServer server = new RemoteIOServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
			new Thread(() =>
			{
				server.Start();
			}).Start();

			Thread.Sleep(1000);

			RemoteIOClient client = new RemoteIOClient("127.0.0.1", 9090);
			bool err = false;
			try
			{
				client.Start("muhammed2", "123123", 2048);
			}
			catch (Exception ex)
			{
				err = ex.Message.StartsWith("NOT_AUTHORIZED");
			}


			client.Dispose();
			server.Dispose();

			Assert.IsTrue(err);
		}

		[Test]
		public void WrongPasswordTest()
		{
			RemoteIOServer server = new RemoteIOServer("muhammed", "123123", 2048, 4, 9090, timeOutAuth: 1);
			new Thread(() =>
			{
				server.Start();
			}).Start();

			Thread.Sleep(1000);

			RemoteIOClient client = new RemoteIOClient("127.0.0.1", 9090);
			bool err = false;
			try
			{
				client.Start("muhammed", "123124", 2048);
			}
			catch (Exception ex)
			{
				err = ex.Message.StartsWith("NOT_AUTHORIZED");
			}


			client.Dispose();
			server.Dispose();

			Assert.IsTrue(err);
		}
    }
}