using System.Collections.Generic;
using tetofo.DesignPattern;
using tetofo.DesignPattern.Impl;
using tetofo.Model;
using tetofo.Service.DAO;
using tetofo.Service.DAO.Impl;
using tetofo.Service.FileSerializer;
using tetofo.Service.FileSerializer.Impl;

namespace tetofo;

public static class Program
{
    public static async Task Main(string[] args) {
        IAsyncFileSerializer asyncFileSerializer = new JSONFileSerializer();
        IDataFactory dataFactory = new DataFactory();
        IAsyncDAO<IData, IData> fileDAO = new FileDAO(asyncFileSerializer, dataFactory);
        await fileDAO.SaveAsync(dataFactory.Create((new HashSet<Tag>{Tag.STRING},"This is test serialization from .NET.",null)));
    }
}
