namespace Creuna.Episerver.RedirectHandler.Core
{
    public class ColorHelper
    {
        public static string GetRedTone(int maxValue, int minValue, int value)
        {
            if (maxValue != 0)
            {
                int calculatedValue = value*255/maxValue;


                return calculatedValue.ToString("x");
            }
            return "00";
        }
    }
}