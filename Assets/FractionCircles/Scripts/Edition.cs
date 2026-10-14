namespace FractionCircles
{
    /// <summary>Which version of the tool this project builds.</summary>
    public static class Edition
    {
        public static bool Premium => true;
        public static bool Basic => !Premium;
    }
}
