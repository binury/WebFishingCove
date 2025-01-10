using Cove.GodotFormat;
using System;

namespace Cove.Server.Chalk
{
    public class ChalkCanvas
    {

        public long canvasID;
        public Dictionary<Vector2, int> chalkImage = new Dictionary<Vector2, int>();

        public ChalkCanvas(long canvasID)
        {
            this.canvasID = canvasID;
        }

        public void drawChalk(Vector2 position, int color)
        {
            int[] allowedCanvas = { 0, 1, 2, 3 };
            if (!Array.Exists(allowedCanvas, element => element == canvasID))
            {
                return;
            }

            chalkImage[position] = color;
        }

        public Dictionary<int, object> getChalkPacket()
        {

            Dictionary<int, object> packet = new Dictionary<int, object>();
            ulong i = 0;
            foreach (KeyValuePair<Vector2, int> entry in chalkImage.ToList())
            {
                Dictionary<int, object> arr = new();
                arr[0] = entry.Key;
                arr[1] = entry.Value;
                packet[(int)i] = arr;
                i++;
            }

            return packet;
        }
        public void chalkUpdate(Dictionary<int, object> packet)
        {
            foreach (KeyValuePair<int, object> entry in packet)
            {
                Dictionary<int, object> arr = (Dictionary<int, object>)entry.Value;
                Vector2 vector2 = (Vector2)arr[0];
                Vector2 pos = new Vector2((int)Math.Round(vector2.x), (int)Math.Round(vector2.y));
                Int64 color = (Int64)arr[1];

                chalkImage[pos] = (int)color;
            }
        }

        public void clearCanvas()
        {
            chalkImage.Clear();
        }
    }
}
