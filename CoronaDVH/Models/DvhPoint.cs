namespace CoronaDVH.Models
{
    public class DvhPoint
    {
        public DvhPoint(float dose, float volume)
        {
            Dose = dose;
            Volume = volume;
        }

        public double Dose { get; set; }
        public double Volume { get; set; }
    }
}