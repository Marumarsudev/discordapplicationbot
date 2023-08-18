using Newtonsoft.Json;

namespace KirnuApplicationBot
{
    public class CustomIDJson
    {
        public string id;
        public string action;

        public CustomIDJson(string _id, string _action)
        {
            id = _id;
            action = _action;
        }

        public string GetJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}