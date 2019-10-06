namespace JoinRpg.Web.Models
{

    public class ErrorViewModel
    {

        public string Title { get; set; }

        public string Message { get; set; }

        public string Description { get; set; }

        public string ReturnLink { get; set; }

        public string ReturnText { get; set; }

        #if DEBUG
        public object Data { get; set; }
        #else
        public object Data
        {
            get { return null; }
            set { value = null; }
        }
        #endif

    }

}
