using Caliburn.Micro;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Size = System.Drawing.Size;

namespace PictureResizer
{
    public class MainViewModel : Screen
    {
        public char pathSeparator => '|';
        private readonly string[] imageExtensions =
            new string[] { ".jpg", ".jpeg", ".gif", ".png", ".bmp" };
        private readonly string[] imageFormatChoices =
            new string[] { "Original", "JPEG", "PNG", "GIF", "BMP" };

        public string TargetConcatFolders { get; set; }
        public string TargetConcatFileNames { get; set; }
        public bool SubFoldersIncluded { get; set; }

        public ObservableCollection<string> ImageFormats => new ObservableCollection<string>(imageFormatChoices);
        public string SelectedImageFormat { get; set; }
        public int JpegQuality { get; set; }

        public bool ResizeByRatio { get; set; }
        public double WidthRatio { get; set; }
        public double HeightRatio { get; set; }
        public bool RatioLocked { get; set; }
        public bool IsHeightRatioEnabled => ResizeByRatio && !RatioLocked;

        public bool ResizeByPixel { get; set; }
        public bool CanResizeByPixel { get; set; }
        public int WidthPixel { get; set; }
        public int HeightPixel { get; set; }
        public bool PixelNotLocked { get; set; }
        public bool PixelLockedByWidth { get; set; }
        public bool PixelLockedByHeight { get; set; }
        public bool IsWidthPixelRatioEnabled => ResizeByPixel && (PixelNotLocked || PixelLockedByWidth);
        public bool IsHeightPixelRatioEnabled => ResizeByPixel && (PixelNotLocked || PixelLockedByHeight);

        public string Message { get; set; }
        public bool ReplaceOriginal { get; set; }
        public bool CanResize { get; set; }

        public List<string> ChosenFolders { get; }
        public List<string> ChosenFileNames { get; }
        public int ChosenFolderCount => ChosenFolders.Count;
        public int ChosenFileCount => ChosenFileNames.Count;

        [DoNotNotify]
        public int OriginalWidthPixel { get; set; }
        [DoNotNotify]
        public int OriginalHeightPixel { get; set; }

        public MainViewModel()
        {
            ChosenFolders = new List<string>();
            ChosenFileNames = new List<string>();
            SetupInitialStates();
            DisplayName = "Picture Resizer";
        }

        private void SetupInitialStates()
        {
            TargetConcatFolders = @"D:\Temp\Images";
            SubFoldersIncluded = true;

            SelectedImageFormat = "JPEG";
            JpegQuality = 95;

            ResizeByRatio = true;
            WidthRatio = 50;
            HeightRatio = 50;
            RatioLocked = true;
            PixelNotLocked = true;

            ReplaceOriginal = false;
        }

        public void OnTargetConcatFoldersChanged()
        {
            // get folder names
            ChosenFolders.Clear();
            Message = "";
            if (!string.IsNullOrWhiteSpace(TargetConcatFolders))
            {
                ChosenFolders.AddRange(TargetConcatFolders.Split(pathSeparator)
                    .Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s) && Directory.Exists(s)));
            }
            if (ChosenFolderCount != 1) // 0 or >1 can't have file name
            {
                TargetConcatFileNames = string.Empty;
            }
            CanResize = ChosenFolderCount != 0;
            Message = ChosenFolderCount != 0 ? "" : "Please select at least one file or one folder.";
        }

        public void OnTargetConcatFileNamesChanged()
        {
            // get file names
            ChosenFileNames.Clear();
            Message = "";
            if (!string.IsNullOrWhiteSpace(TargetConcatFileNames))
            {
                ChosenFileNames.AddRange(TargetConcatFileNames.Split(pathSeparator)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            CanResizeByPixel = ChosenFileCount == 1;

            // check enable/disable, and detect file dimesion if single img
            if (ChosenFileCount == 1)
            {
                var size = GetImageDimension(Path.Combine(TargetConcatFolders, TargetConcatFileNames));
                WidthPixel = size == null ? 0 : ((Size)size).Width;
                HeightPixel = size == null ? 0 : ((Size)size).Height;
                OriginalWidthPixel = WidthPixel;
                OriginalHeightPixel = HeightPixel;
            }
            else if (ChosenFileCount == 0)
            {
                ResizeByRatio = true; // implicitly ResizeByPixel = false;
                OriginalWidthPixel = WidthPixel = OriginalHeightPixel = HeightPixel = 0;
            }
            CanResize = ChosenFolderCount != 0;
            Message = ChosenFolderCount != 0 ? "" : "Please select at least one file or one folder.";
        }

        public void OnWidthPixelChanged()
        {
            if (PixelLockedByWidth)
            {
                HeightPixel = (int)Math.Round((double)WidthPixel / OriginalWidthPixel * OriginalHeightPixel, MidpointRounding.AwayFromZero);
            }
        }

        public void OnHeightPixelChanged()
        {
            if (PixelLockedByHeight)
            {
                WidthPixel = (int)Math.Round((double)HeightPixel / OriginalHeightPixel * OriginalWidthPixel, MidpointRounding.AwayFromZero);
            }
        }

        public void DraggingFolder(DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        public void DetectDroppedFolders(DragEventArgs e)
        {
            CanResize = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (filePaths.Length > 0)
                {
                    if (filePaths.Any(p => !Directory.Exists(p)))
                    {
                        Message = "Not all dropped items are valid directories.";
                        CanResize = false;
                    }
                    else
                    {
                        TargetConcatFileNames = string.Empty;
                        TargetConcatFolders = string.Join(pathSeparator.ToString(), filePaths);
                        Message = "";
                        CanResize = true;
                    }
                }
            }
        }

        public void DraggingFiles(DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        public void DetectDroppedFiles(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (filePaths.Length > 0)
                {
                    if (Directory.Exists(filePaths[0]))
                    {
                        Message = "Not all dropped items are valid files.";
                        CanResize = false;
                    }
                    else
                    {
                        var fileNames = filePaths.Select(fn => fn.Split(Path.DirectorySeparatorChar).LastOrDefault()).ToList();
                        TargetConcatFileNames = string.Join(pathSeparator.ToString(), fileNames);
                        TargetConcatFolders = filePaths[0].Remove(filePaths[0].LastIndexOf(fileNames[0]));
                        // CanResize is determined by above two property changes handler.
                    }
                }
            }
        }

        public Size? GetImageDimension(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    int width = 0;
                    int height = 0;
                    using (var imageStream = File.OpenRead(filePath))
                    {
                        var decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.IgnoreColorProfile,
                            BitmapCacheOption.Default);
                        height = decoder.Frames[0].PixelHeight;
                        width = decoder.Frames[0].PixelWidth;
                    }
                    return new Size(width, height);
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task Resize()
        {
            var files = GetFileInfos();
            Message = "Processing " + files.Count + " pictures.";
            await Task.Run(() =>
            {
                Parallel.ForEach(files, (fileInfo) =>
                {
                    if (fileInfo.Extension.ToLowerInvariant().In(imageExtensions))
                    {
                        var newFilePath = GetNewFilePath(fileInfo);
                        using (Image image = Image.FromFile(fileInfo.FullName))
                        {
                            using (Bitmap resizedImage = DoResize(image))
                            {
                                if (resizedImage == null)
                                {
                                    Message = "Failed to resize image.";
                                    return;
                                }

                                var format = GetImageFormat(SelectedImageFormat, fileInfo.Extension);
                                try
                                {
                                    if (format == ImageFormat.Jpeg)
                                    {
                                        resizedImage.SaveAsJpeg(newFilePath, JpegQuality);
                                    }
                                    else
                                    {
                                        resizedImage.Save(newFilePath, format);
                                    }
                                }
                                catch (Exception e)
                                {
                                    Message = "Failed to save image.";
                                    return;
                                }
                            }
                        }
                        if (ReplaceOriginal)
                        {
                            File.Delete(fileInfo.FullName);
                            File.Move(newFilePath, fileInfo.FullName);
                            Console.WriteLine("Finished: {0}", fileInfo.FullName);
                        }
                    }
                });
            });
            Message = "All " + files.Count + " pictures are processed.";
        }

        private string GetNewFilePath(FileInfo fileInfo, int iteration = 0)
        {
            var fn = fileInfo.Name.Remove(fileInfo.Name.LastIndexOf(fileInfo.Extension));
            var newFilePath = Path.Combine(fileInfo.DirectoryName, fn + "_" + iteration + fileInfo.Extension);
            if (File.Exists(newFilePath))
            {
                return GetNewFilePath(fileInfo, iteration + 1);
            }
            return newFilePath;
        }

        private List<FileInfo> GetFileInfos()
        {
            if (ChosenFolderCount == 0)
            {
                return null;
            }
            else if (ChosenFolderCount == 1 && ChosenFileCount == 1)
            {
                return new List<FileInfo> { new FileInfo(Path.Combine(TargetConcatFolders, TargetConcatFileNames)) };
            }
            else if (ChosenFolderCount == 1 && ChosenFileCount > 1)
            {
                return new List<FileInfo>(ChosenFileNames.Select(fn => new FileInfo(Path.Combine(ChosenFolders[0], fn))).Where(fi => fi.Exists));
            }
            else if (ChosenFolderCount >= 1 && ChosenFileCount == 0)
            {
                var filePaths = new List<string>();
                foreach (var folder in ChosenFolders)
                {
                    if (SubFoldersIncluded)
                    {
                        filePaths.AddRange(Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                            .Where(p => p.EndsWithAny(imageExtensions)));
                    }
                    else
                    {
                        filePaths.AddRange(Directory.GetFiles(folder).Where(p => p.EndsWithAny(imageExtensions)));
                    }
                }
                return filePaths
                    .Select(p => new FileInfo(p)).ToList();
            }
            throw new InvalidOperationException("Invalid info is provided for folder and file names.");
        }

        private Bitmap DoResize(Image image)
        {
            if (ResizeByRatio)
                return ImageHelper.ResizeImage(image, WidthRatio / 100, HeightRatio / 100);
            if (ResizeByPixel)
                return ImageHelper.ResizeImage(image, WidthPixel, HeightPixel);
            return null;
        }

        private ImageFormat GetImageFormat(string specificImageFormat, string originalFileExtension = null)
        {
            var fileExtension = originalFileExtension;
            if (specificImageFormat != "ORIGINAL")
            {
                fileExtension = "." + specificImageFormat.ToLowerInvariant();
            }

            switch (fileExtension)
            {
                case ".jpeg":
                case ".jpg":
                    return ImageFormat.Jpeg;
                case ".gif":
                    return ImageFormat.Gif;
                case ".png":
                    return ImageFormat.Png;
                case ".bmp":
                    return ImageFormat.Bmp;
            }
            return ImageFormat.Bmp;
        }
    }
}
