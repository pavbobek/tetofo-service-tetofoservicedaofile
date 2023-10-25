using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Runtime.ConstrainedExecution;
using System.Text.Json;
using tetofo.DesignPattern;
using tetofo.Model;
using tetofo.Model.Impl;
using tetofo.Service.DAO;
using tetofo.Service.DAO.Exception;
using tetofo.Service.FileSerializer;

namespace tetofo.Service.DAO.Impl;
/// <summary>
/// Provides basic folder implementation for the persist data in files.
/// </summary>
public class FileDAO : IAsyncDAO<IData, IData>
{
    private readonly string _folder;
    private readonly IAsyncFileSerializer _asyncFileSerializer;
    private readonly IDataFactory _dataFactory;

    private static readonly IData DEFAULT_FOLDER = new Data{
        Tags = new HashSet<Tag>{Tag.DIRECTORY_PATH},
        Payload = Path.Combine("/", "exchange"),
        Members = null
    };

    public FileDAO(IAsyncFileSerializer asyncFileSerializer, IDataFactory dataFactory, IData? folder = null) {
        if (folder == null) {
            folder = DEFAULT_FOLDER;
        }
        if (!folder?.Tags?.Contains(Tag.DIRECTORY_PATH) ?? false) {
            throw new DAOException($"Invalid data type to create FileDAO.");
        }
        if (string.IsNullOrEmpty(folder?.Payload)) {
            throw new DAOException($"Path is empty.");
        }
        try {
            Directory.CreateDirectory(folder?.Payload??"");
        }
        catch (System.Exception e) {
            throw new DAOException($"Unable to create directory specified.", e);
        }
        _folder = folder?.Payload??"";
        _asyncFileSerializer = asyncFileSerializer;
        _dataFactory = dataFactory;
    }

    public Task DeleteAsync(IData s)
    {
        if (!HasPersistenceFileTag(s)) {
            throw new DAOException($"Invalid argument for DeleteAsync: {s}, requires Tag.PERSISTENCE_FILE.");
        }
        if (!File.Exists(s.Payload)) {
            throw new DAOException($"Invalid argument for DeleteAsync: {s}, file not exist.");
        }
        try {
            File.Delete(s.Payload);
        }
        catch (System.Exception) {
            throw new DAOException($"Unable to delete file: {s}");
        }
        return Task.CompletedTask;
    }

    public async Task<IData> GetAsync(IData s)
    {
        if (!HasPersistenceFileTag(s)) {
            throw new DAOException($"Invalid argument for GetAsync: {s}, requires Tag.PERSISTENCE_FILE.");
        }
        if (!File.Exists(s.Payload)) {
            throw new DAOException($"Invalid argument for GetAsync: {s}, file not exist.");
        }
        IData? result = await DeserializeFileAsync(s.Payload, _asyncFileSerializer, _dataFactory);
        if (result == null) {
            throw new DAOException($"Unable to deserialize content: {s}.");
        }
        return result;
    }

    public async Task<IList<IData>> GetAllAsync()
    {
        IList<IData> result = new List<IData>();
        foreach(string file in Directory.GetFiles(_folder)) {
            IData? data = await DeserializeFileAsync(file, _asyncFileSerializer, _dataFactory);
            if (data != null) {
                result.Add(data);
            }
        }
        return result;
    }

    public async Task SaveAsync(IData r)
    {
        IData clone = CreateSafeClone(r, _dataFactory);
        string path = Path.Combine(_folder, CreateFileName());
        await _asyncFileSerializer.SerializeToFileAsync(path, clone);
    }

    public Task UpdateAsync(IData r, IData s)
    {
        throw new NotImplementedException();
    }

    private static string CreateFileName() {
        return $"{DateTime.Now:yyyyMMddHHmmssfff}.json";
    }

    private static IData CreateSafeClone(IData r, IDataFactory dataFactory) {
        ISet<Tag>? tags = r.Tags;
        string? payload = r.Payload;
        IList<IData>? members = null;
        if(r.Members != null) {
            members = new List<IData>(r.Members);
            foreach(IData member in r.Members) {
                if (member.Tags?.Contains(Tag.PERSISTENCE_FILE)??false) {
                    members.Remove(member);
                }
            }
        }
        return dataFactory.Create((tags, payload, members));
    }

    private static async Task<IData?> DeserializeFileAsync(string path, IAsyncFileSerializer asyncFileSerializer, IDataFactory dataFactory) {
        IData? content = await asyncFileSerializer.DeserializeFromFileAsync<Data>(path);
        if (content == null) {
            return content;
        }
        IData fileToken = dataFactory.Create((new HashSet<Tag>{Tag.PERSISTENCE_FILE}, path, null));
        IList<IData> members = content?.Members ?? new List<IData>();
        members.Add(fileToken);
        if (content != null) {
            content.Members = members;
        }
        return content;
    }

    private static bool HasPersistenceFileTag(IData data) {
        return data.Tags?.Contains(Tag.PERSISTENCE_FILE)??false;
    }

}