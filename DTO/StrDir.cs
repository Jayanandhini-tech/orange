namespace CMS.Dto
{
    public static class StrDir
    {

        public struct AppType
        {
            public static readonly string KIOSK = "KIOSK";
            public static readonly string POS = "POS";
            public static readonly string VM = "VM";
        }


        public struct OrderStatus
        {
            public static readonly string INITIATED = "INITIATED";
            public static readonly string SUCCESS = "SUCCESS";
            public static readonly string FAILED = "FAILED";
            public static readonly string PAID = "PAID";
        }



        public struct PaymentType
        {
            public static readonly string ACCOUNT = "ACCOUNT";
            public static readonly string UPI = "UPI";
            public static readonly string CASH = "CASH";
            public static readonly string CARD = "CARD";
            public static readonly string COUNTER = "COUNTER";
        }


        public struct AuthPage
        {
            public static readonly string FACE = "FACE";
            public static readonly string IDCARD = "IDCARD";
        }

        public struct AccountPlan
        {
            public static readonly string PREPAID = "PREPAID";
            public static readonly string POSTPAID = "POSTPAID";
        }

        public struct PaymentProvider
        {
            public static readonly string STUDENT = "STUDENT";
            public static readonly string PHONEPE = "PHONEPE";
        }


        public struct TransactionType
        {
            public static readonly string DR = "DR";
            public static readonly string CR = "CR";
        }

        public struct Company
        {

            public static readonly string Name = "Bharath Vending Corporation";
            public static readonly string Address = "16B, E&E Industrial Estate, Civil Aerodrome Post, Sitra, Coimbatore - 641 014.";
            public static readonly string Phone = "+91 80566 80266";
            public static readonly string GstIn = "33AAXFB2859F1Z6";
        }

    }
}
