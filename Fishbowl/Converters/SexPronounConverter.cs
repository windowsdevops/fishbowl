namespace FacebookClient
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using Contigo;

    public class SexPronounConverter : IValueConverter
    {
        private static readonly string[][][] _ProperPossessiveGenderStringTable = new string[][][]
        {
            // non-proper
            new string[][] 
            {
                // non-possessive
                new string[] { "they", "he", "she", },
                // possessive
                new string[] { "their", "his", "her" },
            },
            // proper
            new string[][]
            {
                // non-possessive
                new string[] { "They", "He", "She", },
                // possessive
                new string[] { "Their", "His", "Her" },
            },
        };

        #region IValueConverter Members
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var contact = value as FacebookContact;

            int properPronoun = 0;
            int posessive = 0;
            var strParam = parameter as string;
            if (strParam != null)
            {
                if (strParam.ToLower().Contains("proper"))
                {
                    properPronoun = 1;
                }
                if (strParam.ToLower().Contains("possessive"))
                {
                    posessive = 1;
                }
            }

            int gender = 0;
            if (contact != null)
            {
                if (contact.Sex.ToLower() == "male")
                {
                    gender = 1;
                }
                if (contact.Sex.ToLower() == "female")
                {
                    gender = 2;
                }
            }

            return _ProperPossessiveGenderStringTable[properPronoun][posessive][gender];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}
