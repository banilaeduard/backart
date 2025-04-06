namespace WordDocumentServices
{
    public class ContextMap : Dictionary<string, object>
    {
        public int CurrentIndex = 0;
        private QrCodeService qrCodeSvc = new();

        public ContextMap(Dictionary<string, object> ctx)
        {
            if (ctx != null)
                foreach (var item in ctx)
                {
                    this[item.Key] = item.Value;
                }
        }

        public T GetOrDefault<T>(string key, T defaultValue)
        {
            if (ContainsKey(key))
            {
                return (T)this[key];
            }
            else
            {
                this[key] = defaultValue;
            }
            return defaultValue;
        }

        public void SetValue<T>(string key, T value, bool replace = true)
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

        public Stream GenerateQrCode(string info, int size)
        {
            return qrCodeSvc.GenerateQrCode(info, ZXing.QrCode.Internal.ErrorCorrectionLevel.H, size);
        }
    }
}
