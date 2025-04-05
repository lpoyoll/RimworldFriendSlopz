using MessagePack;

namespace Shared
{
    [MessagePackObject]
    public class PollutionData 
    {
        public PollutionDetails _pollutionData = new PollutionDetails();
    }
}