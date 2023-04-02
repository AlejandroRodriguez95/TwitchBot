using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchBotTest.Classes
{
    public class TimerVisuals
    {
        Bitmap bitmap;
        public TimerVisuals(int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height);
            Graphics graphics = Graphics.FromImage(bitmap);

            Font font = new Font("Arial", 12, FontStyle.Regular);
            SolidBrush brush = new SolidBrush(Color.Black);

            graphics.DrawString("Hello", font, brush, new PointF(width/2, height/2));

            bitmap.Save("test.png", ImageFormat.Png);

            graphics.Dispose();
        }


    }
}
