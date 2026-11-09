using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class LogMessage
    {
        public int Id { get; set; }
        public int IdUser { get; set; }
        public string Message { get; set; }
        public DateTime ExecutedAt { get; set; }

        public LogMessage(int idUser, string message, DateTime executedAt)
        {
            IdUser = idUser;
            Message = message;
            ExecutedAt = executedAt;
            // /cd User: login - asdas; id - 0;
        }
    }
}
