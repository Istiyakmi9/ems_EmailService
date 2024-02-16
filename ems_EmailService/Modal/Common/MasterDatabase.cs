namespace EmailRequest.Modal.Common
{
    public class MasterDatabase
    {
        public string Server { set; get; }
        public int Port { set; get; }
        public string Database { set; get; }
        public string User_Id { set; get; }
        public string Password { set; get; }
        public int Connection_Timeout { set; get; }
        public int Connection_Lifetime { set; get; }
        public int Min_Pool_Size { set; get; }
        public int Max_Pool_Size { set; get; }
        public bool Pooling { set; get; }

        public static string BuildConnectionString(MasterDatabase x)
        {
            return $"server={x.Server};port={x.Port};database={x.Database};User Id={x.User_Id};password={x.Password};Connection Timeout={x.Connection_Timeout};Connection Lifetime={x.Connection_Lifetime};Min Pool Size={x.Min_Pool_Size};Max Pool Size={x.Max_Pool_Size};Pooling={x.Pooling};";
        }
    }
}
