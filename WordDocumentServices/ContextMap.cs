namespace WordDocumentServices
{
    public class ContextMap : Dictionary<string, string>
    {
        public int CurrentIndex = 0;
        public readonly int QRSIZE = 150;
        private QrCodeService qrCodeSvc = new();

        public ContextMap(Dictionary<string, string>? ctx)
        {
            if (ctx != null)
                foreach (var item in ctx)
                {
                    this[item.Key] = item.Value;
                }
        }

        public string GetOrDefault(string key, string defaultValue)
        {
            if (ContainsKey(key))
            {
                return this[key];
            }
            else
            {
                this[key] = defaultValue;
            }
            return defaultValue;
        }

        public void SetValue(string key, string value, bool replace = true)
        {
            if (!ContainsKey(key) || replace)
            {
                this[key] = value!;
            }
        }

        public string GetDots(int number = 10)
        {
            string dots = "";
            for (int i = 0; i < number; i++)
            {
                dots += ".";
            }
            return dots;
        }

        public int IncrementIndex(bool increment = true)
        {
            return increment ? ++CurrentIndex : CurrentIndex;
        }

        public int ResetIndex()
        {
            return CurrentIndex = 0;
        }

        public Stream GenerateQrCode(string title, string info, int size)
        {
            return qrCodeSvc.GenerateQrCode($@"{title} :: {info}", ZXing.QrCode.Internal.ErrorCorrectionLevel.H, size);
        }

        private string WrapInCard(string title, string value)
        {
            return @$"BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
SUMMARY:{title}
DESCRIPTION:{value}
DTSTART:{DateTime.UtcNow.ToString("yyyyMMddTHHmmss")}
END:VEVENT
END:VCALENDAR";
        }
    }
}