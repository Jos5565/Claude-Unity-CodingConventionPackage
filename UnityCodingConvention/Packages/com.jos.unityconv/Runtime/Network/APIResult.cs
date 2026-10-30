namespace UnityConv.Network
{
    public class APIResult
    {
        public bool ok;
        public long statusCode;
        public string text;
        public byte[] bytes;
        public string error;
        public bool canceled; // 취소된 요청 (에러 아님)
    }
}
