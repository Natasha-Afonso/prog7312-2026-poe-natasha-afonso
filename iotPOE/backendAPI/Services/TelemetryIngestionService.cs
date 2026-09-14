using backendAPI.Models;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    public class TelemetryIngestionService
    {

        //jaggered array
                public List<TelemetryPacket<double>> ProcessRawBatches(string[] sensorIds, double[][] rawBatches)
        {
            var packets = new List<TelemetryPacket<double>>();

            //looping through the outer array to get each sensor batch
            for (int i = 0; i < rawBatches.Length; i++)
            {
                double[] sensorBatch = rawBatches[i];

                //go through the inner array for all readings sent by this sensor
                for (int j = 0; j < sensorBatch.Length; j++)
                {
                    //add each reading to the packets list
                    packets.Add(new TelemetryPacket<double>(sensorIds[i], "Reading", sensorBatch[j]));
                }
            }

            return packets;
        }
    }
}