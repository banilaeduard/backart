namespace ServiceImplementation
{
    public static class BarCodeGenerator
    {
        public static string GenerateDataUrlBarCode(string value)
        {
            //var myBarcode = BarcodeWriter.CreateBarcode(value, BarcodeWriterEncoding.Code128);
            //myBarcode.ResizeTo(150, 35);
            //myBarcode.SetMargins(12);
            //return myBarcode.ToDataUrl();
            return value;
        }
    }
}
