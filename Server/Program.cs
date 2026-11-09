using Common;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        public static ApplicationContext _context;
        public static List<User> Users = new List<User>();
        public static IPAddress IpAddress;
        public static int Port;

        static void Main(string[] args)
        {
            _context = new ApplicationContext();
            //_context.Users.Add(new User("klimov", "steps0n", @"C:\Авиатехникум"));
            //_context.SaveChanges();
            try
            {
                Users = _context.Users.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            //Users.Add(new User("klimov", "steps0n", @"\Авиатехникум"));
            Console.Write("Введите IP адрес сервера: ");
            string sIpAddress = Console.ReadLine();
            Console.Write("Введите порт: ");
            string sPort = Console.ReadLine();


            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAddress, out IpAddress))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Данные успешно введены. Запускаю сервер.");
                StartServer();
            }

            Console.Read();
        }

        public static bool AutorizationUser(string login, string password)
        {
            User user = null;
            user = Users.FirstOrDefault(u => u.Login == login && u.Password == password);
            
            return user != null;
        }

        public static List<string> GetDirectory(string src)
        {
            List<string> FoldersFiles = new List<string>();

            if (Directory.Exists(src))
            {
                string[] dirs = Directory.GetDirectories(src);

                foreach (string dir in dirs)
                {
                    string NameDirectory = dir.Replace(src, "");
                    FoldersFiles.Add(NameDirectory + "/");
                }

                string[] files = Directory.GetFiles(src);

                foreach (string file in files)
                {
                    string NameFile = file.Replace(src, "");
                    FoldersFiles.Add(NameFile);
                }
            }

            return FoldersFiles;
        }

        public static void Log(int userId, string message)
        {
            try
            {
                _context.LogMessages.Add(new LogMessage(userId, message, DateTime.Now));
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Не удалось записать лог: " + ex.Message);
            }
        }

        public static void StartServer()
        {
            IPEndPoint endPoint = new IPEndPoint(IpAddress, Port);

            Socket sListener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            sListener.Bind(endPoint);
            sListener.Listen(10);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Сервер запущен.");

            while (true)
            {
                try
                {
                    Socket Handler = sListener.Accept();
                    string Data = null;
                    byte[] Bytes = new byte[10485760];
                    int BytesRec = Handler.Receive(Bytes);
                    Data += Encoding.UTF8.GetString(Bytes, 0, BytesRec);
                    Console.Write("Сообщение от пользователя: " + Data + "\n");
                    string Reply = "";

                    ViewModelSend ViewModelSend = JsonConvert.DeserializeObject<ViewModelSend>(Data);

                    if (ViewModelSend != null)
                    {
                        ViewModelMessage viewModelMessage;

                        string[] DataCommand = ViewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);

                        if (DataCommand[0] != "connect")
                        {
                            Log(ViewModelSend.Id, ViewModelSend.Message);
                        }

                        if (DataCommand[0] == "connect")
                        {
                            var currentUser = Users.FirstOrDefault(u => u.Id == ViewModelSend.Id);
                            string[] DataMessage = ViewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);

                            if (AutorizationUser(DataMessage[1], DataMessage[2]))
                            {
                                //int IdUser = Users.FindIndex(x => x.Login == DataMessage[1] && x.Password == DataMessage[2]);
                                int IdUser = -1;
                                var user1 = Users.FirstOrDefault(x => x.Login == DataMessage[1] && x.Password == DataMessage[2]);
                                if (user1 != null)
                                {
                                    IdUser = user1.Id;
                                }
                                Log(IdUser, ViewModelSend.Message);
                                viewModelMessage = new ViewModelMessage("autorisation", IdUser.ToString());
                            }
                            else
                            {
                                Log(-1, ViewModelSend.Message);
                                viewModelMessage = new ViewModelMessage("message", "Не правильный логин и пароль пользователя.");
                            }

                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else if (DataCommand[0] == "cd")
                        {
                            if (ViewModelSend.Id != -1)
                            {
                                var currentUser = Users.FirstOrDefault(u => u.Id == ViewModelSend.Id);

                                string[] DataMessage = ViewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);

                                List<string> FoldersFiles = new List<string>();

                                if (DataMessage.Length == 1)
                                {
                                    currentUser.Temp_src = currentUser.Src;
                                    FoldersFiles = GetDirectory(currentUser.Src);
                                }
                                else
                                {
                                    string cdFolder = "";

                                    for (int i = 1; i < DataMessage.Length; i++)
                                        if (cdFolder == "")
                                            cdFolder += DataMessage[i];
                                        else
                                            cdFolder += " " + DataMessage[i];

                                    currentUser.Temp_src = currentUser.Temp_src + cdFolder;
                                    FoldersFiles = GetDirectory(currentUser.Temp_src);
                                }

                                if (FoldersFiles.Count == 0)
                                    viewModelMessage = new ViewModelMessage("message", "Директория пуста или не существует.");
                                else
                                    viewModelMessage = new ViewModelMessage("cd", JsonConvert.SerializeObject(FoldersFiles));
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            }

                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else if (DataCommand[0] == "get")
                        {
                            if (ViewModelSend.Id != -1)
                            {
                                var currentUser = Users.FirstOrDefault(u => u.Id == ViewModelSend.Id);
                                string[] DataMessage = ViewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                                string getFile = "";

                                for (int i = 1; i < DataMessage.Length; i++)
                                    if (getFile == "")
                                        getFile += DataMessage[i];
                                    else
                                        getFile += " " + DataMessage[i];

                                byte[] byteFile = File.ReadAllBytes(currentUser.Temp_src + getFile);
                                viewModelMessage = new ViewModelMessage("file", JsonConvert.SerializeObject(byteFile));
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            }

                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else if (DataCommand[0] == "history")
                        {
                            if (ViewModelSend.Id != -1)
                            {
                                var logs = _context.LogMessages.Where(l => l.IdUser == ViewModelSend.Id).OrderBy(l => l.ExecutedAt).Select(l => l.ExecutedAt.ToString("dd.MM.yyyy HH:mm:ss") + " — " + l.Message).ToList();
                                viewModelMessage = new ViewModelMessage("history", JsonConvert.SerializeObject(logs));
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            }

                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else
                        {
                            if (ViewModelSend.Id != -1)
                            {
                                var currentUser = Users.FirstOrDefault(u => u.Id == ViewModelSend.Id);
                                FileInfoFTP SendFileInfo = JsonConvert.DeserializeObject<FileInfoFTP>(ViewModelSend.Message);
                                File.WriteAllBytes(currentUser.Temp_src + @"\" + SendFileInfo.Name, SendFileInfo.Data);
                                viewModelMessage = new ViewModelMessage("message", "Файл загружен");
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            }

                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                    }
                }
                catch (Exception exp)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Что-то пошло не так: " + exp.Message);
                }
            }
        }
    }
}
