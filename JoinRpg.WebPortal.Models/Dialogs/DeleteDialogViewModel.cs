using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Web.Models.Dialogs
{
    public class DeleteDialogViewModel : IProjectIdAware
    {
        public int ProjectId { get; set; }
        public string Title { get; set; } 
    }
}
