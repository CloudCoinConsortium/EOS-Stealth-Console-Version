﻿using System;
using System.Collections.Generic;
using System.Linq;
using CloudCoinCore;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Drawing;
using System.Drawing.Drawing2D;
using ZXing.QrCode;
using ZXing.PDF417;
using ZXing;
using ZXing.Common;
using System.Drawing.Imaging;


namespace CloudCoinClient.CoreClasses
{
    public class FileSystem : IFileSystem
    {


        public FileSystem(string RootPath)
        {
            this.RootPath = RootPath;
            ImportFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_IMPORT + Path.DirectorySeparatorChar;
            ExportFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_EXPORT + Path.DirectorySeparatorChar;
            ImportedFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_IMPORTED + Path.DirectorySeparatorChar;
            TemplateFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_TEMPLATES + Path.DirectorySeparatorChar;
            LanguageFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_LANGUAGE + Path.DirectorySeparatorChar;
            CounterfeitFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_COUNTERFEIT + Path.DirectorySeparatorChar;
            PartialFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_PARTIAL + Path.DirectorySeparatorChar;
            FrackedFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_FRACKED + Path.DirectorySeparatorChar;
            DetectedFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_DETECTED + Path.DirectorySeparatorChar;
            SuspectFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_SUSPECT + Path.DirectorySeparatorChar;
            TrashFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_TRASH + Path.DirectorySeparatorChar;
            BankFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_BANK + Path.DirectorySeparatorChar;
            PreDetectFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_PREDETECT + Path.DirectorySeparatorChar;
            LostFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_LOST + Path.DirectorySeparatorChar;
            RequestsFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_REQUESTS + Path.DirectorySeparatorChar;
            DangerousFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_DANGEROUS + Path.DirectorySeparatorChar;
            LogsFolder = RootPath + Path.DirectorySeparatorChar + Config.TAG_LOGS + Path.DirectorySeparatorChar;
            QRFolder = ImportFolder + Config.TAG_QR + Path.DirectorySeparatorChar;
            BarCodeFolder = ImportFolder + Config.TAG_BARCODE + Path.DirectorySeparatorChar;
            //CSVFolder = ImportFolder + Config.TAG_CSV + Path.DirectorySeparatorChar;

        }
        public override bool CreateFolderStructure()
        {

            // Create the Actual Folder Structure
            return CreateDirectories();
            //return true;
        }

        public void CopyTemplates()
        {
            string[] fileNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (String fileName in fileNames)
            {
                if (fileName.Contains("jpeg") || fileName.Contains("jpg"))
                {

                }
            }
        }

        public bool CreateDirectories()
        {
            // Create Subdirectories as per the RootFolder Location
            // Failure will return false

            try
            {
                Directory.CreateDirectory(RootPath);
                Directory.CreateDirectory(ImportFolder);
                Directory.CreateDirectory(ExportFolder);
                Directory.CreateDirectory(BankFolder);
                Directory.CreateDirectory(ImportedFolder);
                Directory.CreateDirectory(LostFolder);
                Directory.CreateDirectory(TrashFolder);
                Directory.CreateDirectory(SuspectFolder);
                Directory.CreateDirectory(DetectedFolder);
                Directory.CreateDirectory(FrackedFolder);
                Directory.CreateDirectory(TemplateFolder);
                Directory.CreateDirectory(PartialFolder);
                Directory.CreateDirectory(CounterfeitFolder);
                Directory.CreateDirectory(LanguageFolder);
                Directory.CreateDirectory(PreDetectFolder);
                Directory.CreateDirectory(RequestsFolder);
                Directory.CreateDirectory(DangerousFolder);
                Directory.CreateDirectory(LogsFolder);
                Directory.CreateDirectory(QRFolder);
                Directory.CreateDirectory(BarCodeFolder);
                //Directory.CreateDirectory(CSVFolder);

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }


            return true;
        }



        public override void LoadFileSystem()
        {
            importCoins = LoadFolderCoins(ImportFolder);
            //var csvCoins = LoadCoinsByFormat(ImportFolder + Path.DirectorySeparatorChar + "CSV", Formats.CSV);
            var qrCoins = LoadCoinsByFormat(ImportFolder + Path.DirectorySeparatorChar + "QrCodes", Formats.QRCode);
            var BarCodeCoins = LoadCoinsByFormat(ImportFolder + Path.DirectorySeparatorChar + "Barcodes", Formats.BarCode);

            // Add Additional File formats if present
            //importCoins = importCoins.Concat(csvCoins);
            importCoins = importCoins.Concat(BarCodeCoins);
            importCoins = importCoins.Concat(qrCoins);

            Debug.WriteLine("Count -" + importCoins.Count());

            //exportCoins = LoadFolderCoins(ExportFolder);
            bankCoins = LoadFolderCoins(BankFolder);
            lostCoins = LoadFolderCoins(LostFolder);
            //importedCoins = LoadFolderCoins(ImportedFolder);
            //trashCoins = LoadFolderCoins(TrashFolder);
            suspectCoins = LoadFolderCoins(SuspectFolder);
            detectedCoins = LoadFolderCoins(DetectedFolder);
            frackedCoins = LoadFolderCoins(FrackedFolder);
            //LoadFolderCoins(TemplateFolder);
            partialCoins = LoadFolderCoins(PartialFolder);
            //counterfeitCoins = LoadFolderCoins(CounterfeitFolder);
            predetectCoins = LoadFolderCoins(PreDetectFolder);
            dangerousCoins = LoadFolderCoins(DangerousFolder);

        }


        public override void DetectPreProcessing()
        {
            foreach (var coin in importCoins)
            {
                string fileName = GetCoinName(coin.FileName);
                int coinExists = (from x in predetectCoins
                                  where x.sn == coin.sn
                                  select x).Count();
                //if (coinExists > 0)
                //{
                //    string suffix = Utils.RandomString(16);
                //    fileName += suffix.ToLower();
                //}
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new JavaScriptDateTimeConverter());
                serializer.NullValueHandling = NullValueHandling.Ignore;
                Stack stack = new Stack(coin);
                using (StreamWriter sw = new StreamWriter(PreDetectFolder + fileName + ".stack"))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, stack);
                }
            }
        }

        public override void ProcessCoins(IEnumerable<CloudCoin> coins)
        {

            var detectedCoins = LoadFolderCoins(DetectedFolder);


            foreach (var coin in detectedCoins)
            {
                if (coin.PassCount >= CloudCoinCore.Config.PassCount)
                {
                    WriteCoin(coin, BankFolder);
                }
                else
                {
                    WriteCoin(coin, CounterfeitFolder);
                }
            }
        }

        public string GetCoinName(string CoinName)
        {
            return CoinName;
        }
        public void TransferCoins(IEnumerable<CloudCoin> coins, string sourceFolder, string targetFolder, string extension = ".stack")
        {
            var folderCoins = LoadFolderCoins(targetFolder);

            foreach (var coin in coins)
            {
                string fileName = GetCoinName(coin.FileName);
                try
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Converters.Add(new JavaScriptDateTimeConverter());
                    serializer.NullValueHandling = NullValueHandling.Ignore;
                    Stack stack = new Stack(coin);
                    using (StreamWriter sw = new StreamWriter(targetFolder + fileName + extension))
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        serializer.Serialize(writer, stack);
                    }
                    File.Delete(sourceFolder + GetCoinName(coin.FileName) + extension);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }


            }
        }




        public override void ClearCoins(string FolderName)
        {

            var fii = GetFiles(FolderName, CloudCoinCore.Config.allowedExtensions);

            DirectoryInfo di = new DirectoryInfo(FolderName);


            foreach (FileInfo file in fii)
                try
                {
                    file.Attributes = FileAttributes.Normal;
                    File.Delete(file.FullName);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

        }

        public bool WriteTextFile(string fileName, string text)
        {
            try
            {
                StreamWriter OurStream;
                OurStream = File.CreateText(fileName);
                OurStream.Write(text);
                OurStream.Close();
            }
            catch (Exception e)
            {
                // MainWindow.logger.Error(e.Message);
                return false;
            }
            return true;
        }
        public List<FileInfo> GetFiles(string path, params string[] extensions)
        {
            List<FileInfo> list = new List<FileInfo>();
            foreach (string ext in extensions)
                list.AddRange(new DirectoryInfo(path).GetFiles("*" + ext).Where(p =>
                      p.Extension.Equals(ext, StringComparison.CurrentCultureIgnoreCase))
                      .ToArray());
            return list;
        }
        public override void MoveImportedFiles()
        {
            var files = Directory
              .GetFiles(ImportFolder)
              .Where(file => CloudCoinCore.Config.allowedExtensions.Any(file.ToLower().EndsWith))
              .ToList();

            string[] fnames = new string[files.Count()];
            for (int i = 0; i < files.Count(); i++)
            {
                MoveFile(files[i], ImportedFolder + Path.DirectorySeparatorChar + Path.GetFileName(files[i]), FileMoveOptions.Rename);
            }

            var filesqr = Directory
              .GetFiles(ImportFolder + Path.DirectorySeparatorChar + "QrCodes")
              .Where(file => CloudCoinCore.Config.allowedExtensions.Any(file.ToLower().EndsWith))
              .ToList();

            string[] fnamesqr = new string[filesqr.Count()];
            for (int i = 0; i < filesqr.Count(); i++)
            {
                MoveFile(filesqr[i], ImportedFolder + Path.DirectorySeparatorChar + Path.GetFileName(filesqr[i]), FileMoveOptions.Rename);
            }

            var filesbar = Directory
              .GetFiles(ImportFolder + Path.DirectorySeparatorChar + "Barcodes")
              .Where(file => CloudCoinCore.Config.allowedExtensions.Any(file.ToLower().EndsWith))
              .ToList();

            string[] fnamesbar = new string[filesbar.Count()];
            for (int i = 0; i < filesbar.Count(); i++)
            {
                MoveFile(filesbar[i], ImportedFolder + Path.DirectorySeparatorChar + Path.GetFileName(filesbar[i]), FileMoveOptions.Rename);
            }
        }

        public override bool WriteCoinToJpeg(CloudCoin cloudCoin, string TemplateFile, string OutputFile, string tag)
        {
            OutputFile = OutputFile.Replace("\\\\", "\\");
            bool fileSavedSuccessfully = true;

            /* BUILD THE CLOUDCOIN STRING */
            String cloudCoinStr = "01C34A46494600010101006000601D05"; //THUMBNAIL HEADER BYTES
            for (int i = 0; (i < 25); i++)
            {
                cloudCoinStr = cloudCoinStr + cloudCoin.an[i];
            } // end for each an

            //cloudCoinStr += "204f42455920474f4420262044454645415420545952414e545320";// Hex for " OBEY GOD & DEFEAT TYRANTS "
            //cloudCoinStr += "20466f756e6465727320372d352d3137";// Console 7-5-17
            cloudCoinStr += "4c6976652046726565204f7220446965";// Live Free or Die
            cloudCoinStr += "00000000000000000000000000";//Set to unknown so program does not export user data
                                                         // for (int i =0; i < 25; i++) {
                                                         //     switch () { }//end switch pown char
                                                         // }//end for each pown
            cloudCoinStr += "00"; // HC: Has comments. 00 = No
            cloudCoin.CalcExpirationDate();
            cloudCoinStr += cloudCoin.edHex; // 01;//Expiration date Sep 2016 (one month after zero month)
            cloudCoinStr += "01";//  cc.nn;//network number
            String hexSN = cloudCoin.sn.ToString("X6");
            String fullHexSN = "";
            switch (hexSN.Length)
            {
                case 1: fullHexSN = ("00000" + hexSN); break;
                case 2: fullHexSN = ("0000" + hexSN); break;
                case 3: fullHexSN = ("000" + hexSN); break;
                case 4: fullHexSN = ("00" + hexSN); break;
                case 5: fullHexSN = ("0" + hexSN); break;
                case 6: fullHexSN = hexSN; break;
            }
            cloudCoinStr = (cloudCoinStr + fullHexSN);
            /* BYTES THAT WILL GO FROM 04 to 454 (Inclusive)*/
            byte[] ccArray = this.hexStringToByteArray(cloudCoinStr);


            /* READ JPEG TEMPLATE*/
            byte[] jpegBytes = null;

            //jpegBytes = readAllBytes(filePath);
            jpegBytes = File.ReadAllBytes(TemplateFile);

            /* WRITE THE SERIAL NUMBER ON THE JPEG */

            //Bitmap bitmapimage;
            //jpegBytes = readAllBytes(filePath);
            jpegBytes = File.ReadAllBytes(TemplateFile);

            /* WRITE THE SERIAL NUMBER ON THE JPEG */

            Bitmap bitmapimage;

            using (var ms = new MemoryStream(jpegBytes))
            {
                bitmapimage = new Bitmap(ms);
            }

            Graphics graphics = Graphics.FromImage(bitmapimage);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            PointF drawPointAddress = new PointF(30.0F, 25.0F);
            graphics.DrawString(String.Format("{0:N0}", cloudCoin.sn) + " of 16,777,216 on Network: 1", new Font("Arial", 10), Brushes.White, drawPointAddress);

            ImageConverter converter = new ImageConverter();
            byte[] snBytes = (byte[])converter.ConvertTo(bitmapimage, typeof(byte[]));

            List<byte> b1 = new List<byte>(snBytes);
            List<byte> b2 = new List<byte>(ccArray);
            b1.InsertRange(4, b2);

            if (tag == "random")
            {
                Random r = new Random();
                int rInt = r.Next(100000, 1000000); //for ints
                tag = rInt.ToString();
            }

            //string fileName = targetPath;

            string fileName = ExportFolder + cloudCoin.FileName + ".jpg";
            File.WriteAllBytes(OutputFile, b1.ToArray());
            //Console.Out.WriteLine("Writing to " + fileName);
            //CoreLogger.Log("Writing to " + fileName);

            return fileSavedSuccessfully;
        }

        public override bool WriteCoinToQRCode(CloudCoin cloudCoin, string OutputFile, string tag)
        {
            var width = 250; // width of the Qr Code   
            var height = 250; // height of the Qr Code   
            var margin = 0;
            var qrCodeWriter = new ZXing.BarcodeWriterPixelData
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = height,
                    Width = width,
                    Margin = margin
                }
            };
            string coinJson = JsonConvert.SerializeObject(cloudCoin);
            var pixelData = qrCodeWriter.Write(coinJson);
            // creating a bitmap from the raw pixel data; if only black and white colors are used it makes no difference   
            // that the pixel data ist BGRA oriented and the bitmap is initialized with RGB   
            using (var bitmap = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            using (var ms = new MemoryStream())
            {
                var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                try
                {
                    // we assume that the row stride of the bitmap is aligned to 4 byte multiplied by the width of the image   
                    System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
                // save to stream as PNG   
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                bitmap.Save(OutputFile);
            }


            return true;
        }

        public override bool WriteCoinToBARCode(CloudCoin cloudCoin, string OutputFile, string tag)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.PDF_417,
                Options = new EncodingOptions { Width = 200, Height = 50 } //optional
            };
            cloudCoin.pan = null;
            var coinJson = JsonConvert.SerializeObject(cloudCoin);
            var imgBitmap = writer.Write(coinJson);
            using (var stream = new MemoryStream())
            {
                imgBitmap.Save(stream, ImageFormat.Png);
                stream.ToArray();
                imgBitmap.Save(OutputFile);
            }
            return true;
        }
    }


}
