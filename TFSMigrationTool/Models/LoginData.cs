using Microsoft.TeamFoundation.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFSMigrationTool.Models
{
    public class LoginData
    {
        public TfsTeamProjectCollection TFS { get; set;}
        public string Project { get; set; }
    }
}
