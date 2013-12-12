﻿using System;
using System.IO;
using System.Linq;
using DryIoc.AttributedRegistration.UnitTests.CUT;
using DryIoc.MefAttributedModel;
using NUnit.Framework;
using ProtoBuf.Meta;

namespace DryIoc.AttributedRegistration.CompileTimeAssemblyScan.Tests
{
    [TestFixture]
    public class CompileTimeRegistrationInfoSerializationTests
    {
        private const string DATA_FILE = "DryExports.bin";

        private string _originalDirectory;
        private string _temporaryTestDirectory;

        [SetUp]
        public void SetupTestDirectory()
        {
            _temporaryTestDirectory = Path.GetRandomFileName();
            Directory.CreateDirectory(_temporaryTestDirectory);

            _originalDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(_temporaryTestDirectory);
        }

        [TearDown]
        public void TearDownTestDirectory()
        {
            Directory.SetCurrentDirectory(_originalDirectory);
            if (Directory.Exists(_temporaryTestDirectory))
                Directory.Delete(_temporaryTestDirectory, true);
        }

        [Test]
        //[Ignore]
        public void Given_scnanned_assembly_When_serialize_data_Then_deserialize_will_return_the_same_data()
        {
            // Given
            var assembly = typeof(TransientService).Assembly;
            var services = AttributedModel.DiscoverExportsInAssemblies(new[] { assembly }).ToArray();

            // When
            if (File.Exists(DATA_FILE))
                File.Delete(DATA_FILE);

            var model = CreateModel();
            using (var file = File.Create(DATA_FILE))
                model.Serialize(file, services);

            // Then
            var loadedModel = CreateModel();
            TypeExportInfo[] infos;
            using (var file = File.OpenRead(DATA_FILE))
                infos = (TypeExportInfo[])loadedModel.Deserialize(file, null, typeof(TypeExportInfo[]));

            Assert.That(services, Is.EqualTo(infos));
        }

        [Test]
        public void Given_deserialized_data_When_registering_scanned_data_into_container_Then_metadata_should_correctly_registered_too()
        {
            // Given
            var assembly = typeof(TransientService).Assembly;
            var services = AttributedModel.DiscoverExportsInAssemblies(new[] { assembly }).ToArray();

            if (File.Exists(DATA_FILE))
                File.Delete(DATA_FILE);

            var model = CreateModel();
            using (var file = File.Create(DATA_FILE))
                model.Serialize(file, services);

            var loadedModel = CreateModel();
            TypeExportInfo[] infos;
            using (var file = File.OpenRead(DATA_FILE))
                infos = (TypeExportInfo[])loadedModel.Deserialize(file, null, typeof(TypeExportInfo[]));

            // When
            var container = new Container();
            container.RegisterExports(infos);

            // Then
            var factories = container.Resolve<Meta<Func<IServiceWithMetadata>, IViewMetadata>[]>();
            Assert.That(factories.Length, Is.EqualTo(3));
        }

        private static RuntimeTypeModel CreateModel()
        {
            var model = TypeModel.Create();
            model.Add<TypeExportInfo>();
            model.Add<ExportInfo>();
            model.Add<GenericWrapperInfo>();
            model.Add<DecoratorInfo>();
            return model;
        }
    }

    public static class RuntimeTypeModelExt
    {
        public static MetaType Add<T>(this RuntimeTypeModel model)
        {
            var publicFields = typeof(T).GetFields().Select(x => x.Name).ToArray();
            return model.Add(typeof(T), false).Add(publicFields);
        }
    }
}