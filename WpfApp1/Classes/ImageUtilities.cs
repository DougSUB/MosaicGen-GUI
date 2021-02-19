using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace MosaicGen.Classes
{
	public class ImageUtilities
	{
		public ImageUtilities()
		{

		}
		//constants for image orientation data
		private const int OrientationKey = 0x0112;
		private const int NotSpecified = 0;
		private const int NormalOrientation = 1;
		private const int MirrorHorizontal = 2;
		private const int UpsideDown = 3;
		private const int MirrorVertical = 4;
		private const int MirrorHorizontalAndRotateRight = 5;
		private const int RotateLeft = 6;
		private const int MirorHorizontalAndRotateLeft = 7;
		private const int RotateRight = 8;
		public Bitmap ChangeOpacity(Image img, float opacityvalue)
		{

			//Bitmap bmp = new Bitmap(ResizeImageKeepAspectRatio(img, img.Width, img.Height));
			Bitmap bmp = new Bitmap(img.Width, img.Height); // Determining Width and Height of Source Image
			Graphics graphics = Graphics.FromImage(bmp);
			ColorMatrix colormatrix = new ColorMatrix();
			colormatrix.Matrix33 = opacityvalue;
			ImageAttributes imgAttribute = new ImageAttributes();
			imgAttribute.SetColorMatrix(colormatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
			graphics.DrawImage(img, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imgAttribute);
			graphics.Dispose();   // Releasing all resource used by graphics 
			Bitmap result = new Bitmap(bmp);
			return result;
		}
		//copied code snippet from https://stackoverflow.com/questions/33310562/some-images-are-being-rotated-when-resized
		// code takes camera orientation information from the original picture and applies it to the newly created image
		public Bitmap CorrectOrientation(Image source, Image target)
		{
			if (source.PropertyIdList.Contains(OrientationKey))
			{
				var orientation = (int)source.GetPropertyItem(OrientationKey).Value[0];
				switch (orientation)
				{
					case NotSpecified: // Assume it is good.
					case NormalOrientation:
						// No rotation required.
						break;
					case MirrorHorizontal:
						target.RotateFlip(RotateFlipType.RotateNoneFlipX);
						break;
					case UpsideDown:
						target.RotateFlip(RotateFlipType.Rotate180FlipNone);
						break;
					case MirrorVertical:
						target.RotateFlip(RotateFlipType.Rotate180FlipX);
						break;
					case MirrorHorizontalAndRotateRight:
						target.RotateFlip(RotateFlipType.Rotate90FlipX);
						break;
					case RotateLeft:
						target.RotateFlip(RotateFlipType.Rotate90FlipNone);
						break;
					case MirorHorizontalAndRotateLeft:
						target.RotateFlip(RotateFlipType.Rotate270FlipX);
						break;
					case RotateRight:
						target.RotateFlip(RotateFlipType.Rotate270FlipNone);
						break;
					default:
						throw new NotImplementedException("An orientation of " + orientation + " isn't implemented.");
				}
			}
			Bitmap result = new Bitmap(target);
			return result;
		}
		//resize and image while maintianing aspect ratio and orientation
		public Image ResizeImageKeepAspectRatio(Image source, int width, int height)
		{
			Image result = null;

			try
			{
				if (source.Width != width || source.Height != height)
				{
					// Resize image
					float sourceRatio = (float)source.Width / source.Height;

					using (var target = new Bitmap(width, height))
					{
						using (var g = Graphics.FromImage(target))
						{
							g.CompositingQuality = CompositingQuality.HighQuality;
							g.InterpolationMode = InterpolationMode.HighQualityBicubic;
							g.SmoothingMode = SmoothingMode.HighQuality;

							// Scaling
							float scaling;
							float scalingY = (float)source.Height / height;
							float scalingX = (float)source.Width / width;
							if (scalingX < scalingY) scaling = scalingX; else scaling = scalingY;

							int newWidth = (int)(source.Width / scaling);
							int newHeight = (int)(source.Height / scaling);

							// Correct float to int rounding
							if (newWidth < width) newWidth = width;
							if (newHeight < height) newHeight = height;

							// See if image needs to be cropped
							int shiftX = 0;
							int shiftY = 0;

							if (newWidth > width)
							{
								shiftX = (newWidth - width) / 2;
							}

							if (newHeight > height)
							{
								shiftY = (newHeight - height) / 2;
							}
							// Draw image
							g.DrawImage(source, -shiftX, -shiftY, newWidth, newHeight);

						}
						//

						result = CorrectOrientation(source, target);
					}
				}
				else
				{
					// Image size matched the given size
					result = new Bitmap(source);
				}
			}
			catch (Exception)
			{
				result = null;
			}

			return result;
		}
	}
}
