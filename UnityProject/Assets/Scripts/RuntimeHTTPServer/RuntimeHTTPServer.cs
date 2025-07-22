using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class RuntimeHTTPServer : MonoBehaviour
{
    [Header("Server Settings")]
    public int port = 8080;
    public bool autoStart = true;
    
    private HttpListener httpListener;
    private Thread listenerThread;
    private bool isRunning = false;
    private string uploadPath;
    
    #region 公共属性
    
    /// <summary>
    /// 服务器是否正在运行
    /// </summary>
    public bool IsRunning => isRunning;
    
    /// <summary>
    /// 获取上传路径
    /// </summary>
    /// <returns>上传目录路径</returns>
    public string GetUploadPath() => uploadPath;
    
    /// <summary>
    /// 获取局域网URL
    /// </summary>
    /// <returns>局域网访问URL</returns>
    public string GetLANURL()
    {
        string localIP = GetLocalIPAddress();
        return !string.IsNullOrEmpty(localIP) ? $"http://{localIP}:{port}/" : "";
    }
    
    #endregion
    
    void Start()
    {
        // 设置上传路径为persistentDataPath/Upload（运行时可写目录）
        uploadPath = Path.Combine(Application.persistentDataPath, "Upload");
        
        // 确保Upload目录存在
        try
        {
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
                Debug.Log($"Created upload directory: {uploadPath}");
            }
            else
            {
                Debug.Log($"Upload directory already exists: {uploadPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create upload directory: {ex.Message}");
        }
        
        if (autoStart)
        {
            StartServer();
        }
    }
    
    void OnDestroy()
    {
        StopServer();
    }
    
    void OnApplicationQuit()
    {
        StopServer();
    }
    
    [ContextMenu("Start Server")]
    public void StartServer()
    {
        if (isRunning)
        {
            Debug.LogWarning("HTTP Server is already running!");
            return;
        }
        
        try
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://*:{port}/");
            httpListener.Start();
            
            isRunning = true;
            listenerThread = new Thread(HandleRequests) { IsBackground = true };
            listenerThread.Start();
            
            Debug.Log($"Runtime HTTP Server started on port {port}");
            Debug.Log($"Upload URL: http://localhost:{port}/");
            
            // 显示局域网IP
            string localIP = GetLocalIPAddress();
            if (!string.IsNullOrEmpty(localIP))
            {
                Debug.Log($"LAN Upload URL: http://{localIP}:{port}/");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to start server: {ex.Message}");
            isRunning = false;
        }
    }
    
    [ContextMenu("Stop Server")]
    public void StopServer()
    {
        if (!isRunning) return;
        
        isRunning = false;
        
        try
        {
            httpListener?.Stop();
            httpListener?.Close();
            listenerThread?.Join(1000);
            Debug.Log("HTTP Server stopped");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error stopping server: {ex.Message}");
        }
    }
    
    private void HandleRequests()
    {
        while (isRunning && httpListener.IsListening)
        {
            try
            {
                var context = httpListener.GetContext();
                ProcessRequest(context);
            }
            catch (Exception ex)
            {
                if (isRunning)
                {
                    Debug.LogError($"Request handling error: {ex.Message}");
                }
            }
        }
    }
    
    private void ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        
        try
        {
            if (request.HttpMethod == "GET")
            {
                if (request.Url.AbsolutePath == "/")
                {
                    // 返回上传页面
                    SendUploadPage(response);
                }
                else if (request.Url.AbsolutePath == "/api/files")
                {
                    // 获取文件列表
                    HandleGetFileList(request, response);
                }

                else
                {
                    // 404
                    response.StatusCode = 404;
                    SendTextResponse(response, "Not Found");
                }
            }
            else if (request.HttpMethod == "POST")
            {
                if (request.Url.AbsolutePath == "/upload")
                {
                    // 处理文件上传
                    HandleFileUpload(request, response);
                }
                else if (request.Url.AbsolutePath == "/api/delete")
                {
                    // 删除文件
                    HandleDeleteFile(request, response);
                }
                else
                {
                    // 404
                    response.StatusCode = 404;
                    SendTextResponse(response, "Not Found");
                }
            }
            else
            {
                // 405
                response.StatusCode = 405;
                SendTextResponse(response, "Method Not Allowed");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing request: {ex.Message}");
            response.StatusCode = 500;
            SendTextResponse(response, "Internal Server Error");
        }
    }
    
    private void SendUploadPage(HttpListenerResponse response)
    {
        try
        {
            string htmlFilePath = Path.Combine(Application.streamingAssetsPath, "upload.html");
            
            if (File.Exists(htmlFilePath))
            {
                string html = File.ReadAllText(htmlFilePath, Encoding.UTF8);
                byte[] buffer = Encoding.UTF8.GetBytes(html);
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                // 如果HTML文件不存在，返回简单的错误页面
                string errorHtml = @"<!DOCTYPE html>
<html><head><title>Error</title></head>
<body><h1>Upload page not found</h1>
<p>Please ensure upload.html exists in StreamingAssets directory.</p></body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading upload page: {ex.Message}");
            response.StatusCode = 500;
            SendTextResponse(response, "Internal server error");
        }
        finally
        {
            response.OutputStream.Close();
        }
    }
    
    private void HandleFileUpload(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            string boundary = GetBoundary(request.ContentType);
            if (string.IsNullOrEmpty(boundary))
            {
                response.StatusCode = 400;
                var errorResult = new UploadResponse
                {
                    success = false,
                    fileCount = 0,
                    message = "Invalid content type"
                };
                SendJsonResponse(response, errorResult);
                return;
            }
            
            var uploadedFiles = ParseMultipartFormData(request.InputStream, boundary);
            int fileCount = 0;
            
            foreach (var file in uploadedFiles)
            {
                // 验证文件类型，只允许MP3文件
                string extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".mp3")
                {
                    Debug.LogWarning($"Rejected non-MP3 file: {file.FileName}");
                    continue;
                }
                
                string fileName = SanitizeFileName(file.FileName);
                string filePath = Path.Combine(uploadPath, fileName);
                
                File.WriteAllBytes(filePath, file.Data);
                fileCount++;
                
                Debug.Log($"MP3 file uploaded: {fileName} ({file.Data.Length} bytes) -> {filePath}");
            }
            
            var uploadResult = new UploadResponse
            {
                success = true,
                fileCount = fileCount,
                message = "Files uploaded successfully"
            };
            SendJsonResponse(response, uploadResult);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Upload error: {ex.Message}");
            var errorResult = new UploadResponse
            {
                success = false,
                fileCount = 0,
                message = ex.Message
            };
            SendJsonResponse(response, errorResult);
        }
    }
    
    private void HandleGetFileList(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            var files = new List<FileInfoData>();
            
            if (Directory.Exists(uploadPath))
            {
                var fileInfos = Directory.GetFiles(uploadPath, "*", SearchOption.AllDirectories);
                
                foreach (var filePath in fileInfos)
                {
                    var fileInfo = new FileInfo(filePath);
                    var relativePath = GetRelativePath(uploadPath, filePath);
                    
                    files.Add(new FileInfoData
                    {
                        name = Path.GetFileName(filePath),
                        path = relativePath.Replace('\\', '/'),
                        size = fileInfo.Length,
                        sizeFormatted = FormatFileSize(fileInfo.Length),
                        lastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        extension = Path.GetExtension(filePath).ToLower()
                    });
                }
            }
            
            var result = new FileListResponse
            {
                success = true,
                files = files.ToArray(),
                totalCount = files.Count
            };
            SendJsonResponse(response, result);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Get file list error: {ex.Message}");
            var errorResult = new FileListResponse
            {
                success = false,
                files = new FileInfoData[0],
                totalCount = 0
            };
            SendJsonResponse(response, errorResult);
        }
    }
    
    private void HandleDeleteFile(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            using (var reader = new StreamReader(request.InputStream))
            {
                var body = reader.ReadToEnd();
                var data = JsonUtility.FromJson<DeleteFileRequest>(body);
                
                if (string.IsNullOrEmpty(data.fileName))
                {
                    response.StatusCode = 400;
                    SendJsonResponse(response, new { success = false, message = "File name is required" });
                    return;
                }
                
                string filePath = Path.Combine(uploadPath, data.fileName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"File deleted: {data.fileName}");
                    var successResult = new DeleteResponse
                    {
                        success = true,
                        message = $"File '{data.fileName}' deleted successfully"
                    };
                    SendJsonResponse(response, successResult);
                }
                else
                {
                    response.StatusCode = 404;
                    var errorResult = new DeleteResponse
                    {
                        success = false,
                        message = $"File '{data.fileName}' not found"
                    };
                    SendJsonResponse(response, errorResult);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Delete file error: {ex.Message}");
            response.StatusCode = 500;
            var errorResult = new DeleteResponse
            {
                success = false,
                message = ex.Message
            };
            SendJsonResponse(response, errorResult);
        }
    }
    

    private string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
    
    private string GetBoundary(string contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return null;
        
        int boundaryIndex = contentType.IndexOf("boundary=");
        if (boundaryIndex == -1) return null;
        
        return "--" + contentType.Substring(boundaryIndex + 9);
    }
    
    private List<UploadedFile> ParseMultipartFormData(Stream inputStream, string boundary)
    {
        var files = new List<UploadedFile>();
        
        // 读取所有数据到内存
        var memoryStream = new MemoryStream();
        inputStream.CopyTo(memoryStream);
        var data = memoryStream.ToArray();
        memoryStream.Close();
        
        var boundaryBytes = Encoding.UTF8.GetBytes(boundary);
        var parts = SplitByteArray(data, boundaryBytes);
        
        foreach (var part in parts)
        {
            if (part.Length < 4) continue;
            
            string partString = Encoding.UTF8.GetString(part);
            if (!partString.Contains("Content-Disposition") || !partString.Contains("filename=")) continue;
            
            // 提取文件名
            int filenameStart = partString.IndexOf("filename=\"") + 10;
            int filenameEnd = partString.IndexOf("\"", filenameStart);
            if (filenameStart < 10 || filenameEnd == -1) continue;
            
            string fileName = partString.Substring(filenameStart, filenameEnd - filenameStart);
            if (string.IsNullOrEmpty(fileName)) continue;
            
            // 找到文件数据开始位置
            var headerEndBytes = Encoding.UTF8.GetBytes("\r\n\r\n");
            int headerEnd = FindByteSequence(part, headerEndBytes);
            if (headerEnd == -1) continue;
            
            int dataStart = headerEnd + 4;
            int dataLength = part.Length - dataStart - 2; // 减去结尾的\r\n
            if (dataLength > 0)
            {
                byte[] fileData = new byte[dataLength];
                Array.Copy(part, dataStart, fileData, 0, dataLength);
                
                files.Add(new UploadedFile { FileName = fileName, Data = fileData });
            }
        }
        
        return files;
    }
    
    private byte[][] SplitByteArray(byte[] data, byte[] separator)
    {
        var parts = new List<byte[]>();
        int start = 0;
        
        for (int i = 0; i <= data.Length - separator.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < separator.Length; j++)
            {
                if (data[i + j] != separator[j])
                {
                    match = false;
                    break;
                }
            }
            
            if (match)
            {
                if (i > start)
                {
                    byte[] part = new byte[i - start];
                    Array.Copy(data, start, part, 0, i - start);
                    parts.Add(part);
                }
                start = i + separator.Length;
                i += separator.Length - 1;
            }
        }
        
        if (start < data.Length)
        {
            byte[] lastPart = new byte[data.Length - start];
            Array.Copy(data, start, lastPart, 0, data.Length - start);
            parts.Add(lastPart);
        }
        
        return parts.ToArray();
    }
    
    private int FindByteSequence(byte[] data, byte[] sequence)
    {
        for (int i = 0; i <= data.Length - sequence.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < sequence.Length; j++)
            {
                if (data[i + j] != sequence[j])
                {
                    found = false;
                    break;
                }
            }
            if (found) return i;
        }
        return -1;
    }
    
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }
    
    private void SendTextResponse(HttpListenerResponse response, string text)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(text);
        
        response.ContentType = "text/plain";
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }
    
    private void SendJsonResponse(HttpListenerResponse response, object obj)
    {
        try
        {
            string json = JsonUtility.ToJson(obj);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Debug.LogError($"JSON response error: {ex.Message}");
            response.StatusCode = 500;
        }
        finally
        {
            response.OutputStream.Close();
        }
    }
    
    private string GetRelativePath(string basePath, string fullPath)
    {
        if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(fullPath))
            return fullPath;
            
        basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        fullPath = Path.GetFullPath(fullPath);
        
        if (fullPath.StartsWith(basePath + Path.DirectorySeparatorChar))
        {
            return fullPath.Substring(basePath.Length + 1);
        }
        
        return Path.GetFileName(fullPath);
    }
    
    private string GetLocalIPAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not get local IP: {ex.Message}");
        }
        return null;
    }
    
    [System.Serializable]
    public class UploadedFile
    {
        public string FileName;
        public byte[] Data;
    }
    
    [System.Serializable]
    public class DeleteFileRequest
    {
        public string fileName;
    }
    
    [System.Serializable]
    public class FileListResponse
    {
        public bool success;
        public FileInfoData[] files;
        public int totalCount;
    }
    
    [System.Serializable]
    public class FileInfoData
    {
        public string name;
        public string path;
        public long size;
        public string sizeFormatted;
        public string lastModified;
        public string extension;
    }
    
    [System.Serializable]
    public class UploadResponse
    {
        public bool success;
        public int fileCount;
        public string message;
    }
    
    [System.Serializable]
    public class DeleteResponse
    {
        public bool success;
        public string message;
    }
}