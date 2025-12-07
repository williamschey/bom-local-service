namespace BomLocalService.Utilities;

/// <summary>
/// Utility methods for cache folder validation and operations.
/// </summary>
public static class CacheHelper
{
    /// <summary>
    /// Checks if a cache folder is complete (has all required files).
    /// A complete cache folder must have:
    /// - All 7 frame images (frame_0.png through frame_6.png)
    /// - metadata.json file
    /// - frames.json file
    /// </summary>
    /// <param name="cacheFolderPath">The path to the cache folder to check</param>
    /// <returns>True if the folder is complete, false otherwise</returns>
    public static bool IsCacheFolderComplete(string cacheFolderPath)
    {
        if (string.IsNullOrEmpty(cacheFolderPath) || !Directory.Exists(cacheFolderPath))
        {
            return false;
        }

        // Check for all 7 frames
        for (int i = 0; i < 7; i++)
        {
            var framePath = FilePathHelper.GetFrameFilePath(cacheFolderPath, i);
            if (!File.Exists(framePath))
            {
                return false;
            }
        }

        // Check for metadata.json
        var metadataPath = FilePathHelper.GetMetadataFilePath(cacheFolderPath);
        if (!File.Exists(metadataPath))
        {
            return false;
        }

        // Check for frames.json
        var framesMetadataPath = FilePathHelper.GetFramesMetadataFilePath(cacheFolderPath);
        if (!File.Exists(framesMetadataPath))
        {
            return false;
        }

        return true;
    }
}

