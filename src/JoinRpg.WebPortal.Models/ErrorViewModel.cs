namespace JoinRpg.Web.Models
{
    public class ErrorViewModel
    {

        public string Title { get; set; }

        public string Message { get; set; }

        public string Description { get; set; }

        public string ReturnLink { get; set; }

        public string ReturnText { get; set; }

        public bool Debug { get; set; }

        public object Data { get; set; }

        public ErrorViewModel() =>
#if DEBUG
            Debug = true;
#endif

    }
}
