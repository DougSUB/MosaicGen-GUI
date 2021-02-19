using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.ComponentModel;
using System.Threading;

namespace MosaicGen.Classes
{
	public class MosaicEngine
	{
		public MosaicEngine(string mosaicDirectory, string mosaicBaseName)
		{
			MosaicDirectory = mosaicDirectory;
			MosaicBaseName = mosaicBaseName;
		}

		public string MosaicDirectory { get; set; }
		public string MosaicBaseName { get; set; }
		public string MosaicFullPathAddress
		{
			get
			{
				return Path.Combine(MosaicDirectory, MosaicBaseName);
			}
		}

		private ImageUtilities imageUtilities = new ImageUtilities();
		//public int AddImageToDictionaryProgress { get; set; }
		//public int PlaceImageOnMosaicProgress { get; set; }

		// creates a List of Colors for the specified JPG.  (scaled down by 10 to increase performance)
		private List<Color> RGBPixelColorList(string fullPathAddress)
		{
			using (Bitmap bitmapFile = new Bitmap(fullPathAddress))
			{
				//Change scaling to increase performance
				using (Bitmap smallBitmapFile = new Bitmap(bitmapFile, new Size(bitmapFile.Width / 10, bitmapFile.Height / 10)))
				{
					List<Color> pixelColors = new List<Color>();

					//Parallel.For(0, smallBitmapFile.Width, i =>
					for (int i = 0; i < smallBitmapFile.Width; i++)
					{
						for (int j = 0; j < smallBitmapFile.Height; j++)
						{
							pixelColors.Add(smallBitmapFile.GetPixel(i, j));
						}
					}
					return pixelColors;
				}

			}

		}

		// creates an average Color based on the RGPPixelColorList of an image
		private Color ImageToSingleRGBPixel(string fullPathAddress)
		{
			long totalA = 0;
			long totalR = 0;
			long totalG = 0;
			long totalB = 0;
			List<Color> temp = new List<Color>(RGBPixelColorList(fullPathAddress));
			foreach (Color pixel in temp)
			{
				totalA += pixel.A * pixel.A;
				totalR += pixel.R * pixel.R;
				totalG += pixel.G * pixel.G;
				totalB += pixel.B * pixel.B;
			}
			byte rmsA = (byte)Math.Sqrt(totalA / temp.Count);
			byte rmsR = (byte)Math.Sqrt(totalR / temp.Count);
			byte rmsG = (byte)Math.Sqrt(totalG / temp.Count);
			byte rmsB = (byte)Math.Sqrt(totalB / temp.Count);
			return Color.FromArgb(rmsA, rmsR, rmsG, rmsB);
		}

		//return a count of images in active directory
		private int DirectoryImageCount()
		{
			return Directory.GetFiles(MosaicDirectory).Length;
		}

		//return dictionary with string x,y coordinate index and color
		private Dictionary<string, Color> AddToImageDictionary(BackgroundWorker bw)
		{

			Dictionary<string, Color> imageDic = new Dictionary<string, Color>();
			List<string> jpgList = new List<string>(Directory.GetFiles(MosaicDirectory, "*.jpg"));
			List<string> jpegList = new List<string>(Directory.GetFiles(MosaicDirectory, "*.jpeg"));
			List<string> pngList = new List<string>(Directory.GetFiles(MosaicDirectory, "*.png"));
			List<string> myDirectory = new List<string>(jpgList);
			foreach (string jpeg in jpegList)
			{
				myDirectory.Add(jpeg);
			}
			foreach(string png in pngList)
			{
				myDirectory.Add(png);
			}
			int imageCounter = 0;

			Parallel.For(0, myDirectory.Count, i =>
			{
				using (Bitmap current = new Bitmap(Path.Combine(MosaicDirectory, myDirectory[i])))
				{
					imageDic.Add(myDirectory[i], ImageToSingleRGBPixel(Path.Combine(MosaicDirectory, myDirectory[i])));
					imageCounter++;
					bw.ReportProgress(imageCounter * 100 / myDirectory.Count, "Processing Images");

					Console.WriteLine($"{imageCounter}/{myDirectory.Count} : Added ~~ {myDirectory[i].Substring(myDirectory[i].LastIndexOf(@"\") + 1)} ~~ to your dictionary.");
				}
			});


			return imageDic;
		
		}

		//creates a Dictionary of pixel keys for mosaic;
		//keys = future mosaic image coordinates
		//value = color
		private Dictionary<int[], Color> MosaicRGBIndex()
		{
			using (Bitmap temp = new Bitmap(MosaicFullPathAddress))
			{
				int scale = 1;
				int width = temp.Width / scale;
				int height = temp.Height / scale;
				while (width * height > DirectoryImageCount())
				{
					scale++;
					width = temp.Width / scale;
					height = temp.Height / scale;
				}
				using (Bitmap bitmapFile = new Bitmap(imageUtilities.ResizeImageKeepAspectRatio(temp, width, height)))
				{
					Dictionary<int[], Color> imageColorDic = new Dictionary<int[], Color>();
					for (int i = 0; i < bitmapFile.Width; i++)
					{
						for (int j = 0; j < bitmapFile.Height; j++)
						{
							int[] coordinates = new int[2] { i, j };
							imageColorDic.Add(coordinates, bitmapFile.GetPixel(i, j));
						}
					}
					//bitmapFile.Save("index.png");
					//bitmapFile.Dispose();
					return RandomizeDictionaryOrder(imageColorDic);
				}

			}

		}

		public Dictionary<int[], Color> RandomizeDictionaryOrder(Dictionary<int[], Color> intColorDic)
		{
			Random r = new Random();
			return intColorDic.OrderBy(x => r.Next()).ToDictionary(item => item.Key, item => item.Value);
		}

		//creates a Dictionary of string x,y pixel coordinates and cooresponding file names for best match pictures
		//Classic 'Assignment Problem' scenario
		public Dictionary<int[], string> CreateMosaicDictionaryFromDirectory(BackgroundWorker bw)
		{
			Dictionary<string, Color> jpgDic = new Dictionary<string, Color>(AddToImageDictionary(bw));
			Console.WriteLine();
			Console.WriteLine("Generated RGB Indexes for reference image:");
			foreach (KeyValuePair<int[], Color> pixelDef in MosaicRGBIndex())
			{
				Console.WriteLine($"{pixelDef.Key[0]}, {pixelDef.Key[1]}, {pixelDef.Value}");
			}
			Dictionary<int[], string> mosaicAssignment = new Dictionary<int[], string>();
			Dictionary<int[], Color> mosaicRGBIndex = new Dictionary<int[], Color>(MosaicRGBIndex());
			Dictionary<string, double> usedPictures = new Dictionary<string, double>();
			RandomizeDictionaryOrder(mosaicRGBIndex);
			List<double> totalLikeness = new List<double>();
			foreach (KeyValuePair<int[], Color> coordinateXY in mosaicRGBIndex)
			{
				Dictionary<string, double> likeness = new Dictionary<string, double>();
				foreach (KeyValuePair<string, Color> file in jpgDic)
				{
					if (!usedPictures.ContainsKey(file.Key))
					{
						//color distance algorithm take from https://www.compuphase.com/cmetric.htm
						long meanR = ((file.Value.R + coordinateXY.Value.R) / 2);
						long r = (long)file.Value.R - (long)coordinateXY.Value.R;
						long g = (long)file.Value.G - (long)coordinateXY.Value.G;
						long b = (long)file.Value.B - (long)coordinateXY.Value.B;

						double colorDistance = Math.Sqrt((((512 + meanR) * r * r) >> 8) + 4 * g * g + (((767 - meanR) * b * b) >> 8));
						//double colorDistance = Math.Sqrt((r * r) + (g * g) + (b * b));
						//alternative color matching algorithm
						likeness.Add(file.Key, colorDistance);
					}
				}
				double mostAlikeValue = 10000000;
				string mostAlikeKey = "";
				foreach (KeyValuePair<string, double> entry in likeness)
				{
					if (entry.Value < mostAlikeValue)
					{
						mostAlikeValue = entry.Value;
						mostAlikeKey = entry.Key;
					}

				}
				totalLikeness.Add(mostAlikeValue);
				usedPictures.Add(mostAlikeKey, mostAlikeValue);
				mosaicAssignment.Add(coordinateXY.Key, mostAlikeKey);
			}
			return mosaicAssignment;
		}


		//specify the save name and directory for mosaic
		//optimizes picture sizes to best match base image resolution (minimum 50x50 image)
		public Bitmap CreateMosaicFromDirectoryImages(BackgroundWorker bw)
		{
			Dictionary<int[], string> myMosiacInstructions = new Dictionary<int[], string>(CreateMosaicDictionaryFromDirectory(bw));
			Console.WriteLine();
			Console.WriteLine("Coordinates and best match image references for mosaic:");
			int baseImageWidth = Image.FromFile(MosaicFullPathAddress).Width;
			int baseImageHeight = Image.FromFile(MosaicFullPathAddress).Height;
			int maxKey0 = 1;
			int maxKey1 = 1;

			foreach (KeyValuePair<int[], string> image in myMosiacInstructions)
			{
				Console.WriteLine($"{image.Key[0]}, {image.Key[1]}, {image.Value}");
				if (image.Key[0] > maxKey0)
				{
					maxKey0 = image.Key[0];
				}
				if (image.Key[1] > maxKey1)
				{
					maxKey1 = image.Key[1];
				}
			}
			int pixelImageRes;
			if (maxKey0 >= maxKey1)
			{
				pixelImageRes = baseImageHeight / (maxKey1 + 1);
			}
			else
			{
				pixelImageRes = baseImageWidth / (maxKey0 + 1);
			}
			if (pixelImageRes < 50)
			{
				pixelImageRes = 50;
			}

			int maxWidth = pixelImageRes * (maxKey0 + 1);
			int maxHeight = pixelImageRes * (maxKey1 + 1);

			using (Bitmap mosaic = new Bitmap(maxWidth, maxHeight))
			{
				int imageCounter = 1;
				//PlaceImageOnMosaicProgress = imageCounter / myMosiacInstructions.Count * 100;
				Dictionary<int[], Image> temp = new Dictionary<int[], Image>();

				Parallel.ForEach(myMosiacInstructions, image =>
				{
					string fullPath = Path.Combine(image.Value.Substring(0, image.Value.LastIndexOf("\\") + 1), image.Value.Substring(image.Value.LastIndexOf("\\") + 1));
					Image resize = imageUtilities.ResizeImageKeepAspectRatio(Image.FromFile(fullPath), pixelImageRes, pixelImageRes);
					temp.Add(image.Key, resize);
					int percentProgress = imageCounter * 100 / myMosiacInstructions.Count;
					bw.ReportProgress(percentProgress, "Organizing and Cropping Images");
					Console.WriteLine($"{imageCounter++}/{myMosiacInstructions.Count} complete");
				});

				int pixelCounter = 0;
				foreach (KeyValuePair<int[], Image> image in temp)
				{
					Graphics.FromImage(mosaic).DrawImage(image.Value, image.Key[0] * pixelImageRes, image.Key[1] * pixelImageRes);
					pixelCounter++;
					int percentProgress = pixelCounter * 100 / temp.Count;
					//bw.ReportProgress(percentProgress, "Generating Mosaic");
				}
				Bitmap tempp = new Bitmap(mosaic);
				return tempp;
			}
				
		}

		public Bitmap CreateMosaicWithBaseImageOverlay(float opacityValue,BackgroundWorker bw)
		{
			using (Bitmap tempMosaicBaseImage = new Bitmap(Image.FromFile(MosaicFullPathAddress)))
			{
				using (Bitmap mosaicBaseImage = new Bitmap(imageUtilities.CorrectOrientation(Image.FromFile(MosaicFullPathAddress), tempMosaicBaseImage)))
				{
					mosaicBaseImage.Save("baseimage.png");
					using (Bitmap mosaic = new Bitmap(CreateMosaicFromDirectoryImages(bw)))
					{
						bw.ReportProgress(25, "Generating Mosaic");
						mosaic.Save("mosaic.png");
						bw.ReportProgress(50, "Generating Mosaic");
						int width = mosaic.Width;
						int height = mosaic.Height;
						using (Bitmap resize = new Bitmap(imageUtilities.ResizeImageKeepAspectRatio(mosaicBaseImage, width, height)))
						{
							using (Bitmap temp = imageUtilities.ChangeOpacity(mosaic, opacityValue))
							{
								Graphics.FromImage(resize).DrawImage(temp, 0, 0);
								bw.ReportProgress(75, "Combining Mosaic with Base Image");
								resize.Save("final mosaic.png");
								bw.ReportProgress(100, "Displaying Results");
								bw.CancelAsync();
								return resize;
							}
						}

					}
				}
			}
		}

		public Bitmap ChangeOpacityOfMosaicFromFiles(float opacityValue)
		{
			using (Bitmap _base = new Bitmap(Image.FromFile(Directory.GetCurrentDirectory() + "\\baseimage.png")))
			{
				
				using (Bitmap _mosaicImage = new Bitmap(Image.FromFile(Directory.GetCurrentDirectory() + "\\mosaic.png")))
				{
					int width = _mosaicImage.Width;
					int height = _mosaicImage.Height;
					using(Bitmap resize = new Bitmap(imageUtilities.ResizeImageKeepAspectRatio(_base, width, height)))
					{
						using (Bitmap temp = imageUtilities.ChangeOpacity(_mosaicImage, opacityValue))
						{
							Graphics.FromImage(resize).DrawImage(temp, 0, 0);
							resize.Save("final mosaic.png");
							return resize;
						}
					}

				}
			}
		}
	}
}
