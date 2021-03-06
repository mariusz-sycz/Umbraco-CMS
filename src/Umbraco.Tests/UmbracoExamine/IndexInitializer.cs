﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Examine;
using Examine.LuceneEngine.Config;
using Examine.LuceneEngine.Providers;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Moq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Persistence.Querying;
using Umbraco.Core.Persistence.SqlSyntax;
using Umbraco.Core.Services;
using UmbracoExamine;
using UmbracoExamine.Config;
using UmbracoExamine.DataServices;
using IContentService = Umbraco.Core.Services.IContentService;
using IMediaService = Umbraco.Core.Services.IMediaService;
using Version = Lucene.Net.Util.Version;

namespace Umbraco.Tests.UmbracoExamine
{
    /// <summary>
    /// Used internally by test classes to initialize a new index from the template
    /// </summary>
    internal static class IndexInitializer
    {
        public static UmbracoContentIndexer GetUmbracoIndexer(
                IndexWriter writer,
                Analyzer analyzer = null,
                IDataService dataService = null,
                IContentService contentService = null,
                IMediaService mediaService = null,
                IDataTypeService dataTypeService = null,
                IMemberService memberService = null,
                IUserService userService = null,
                IContentTypeService contentTypeService = null, 
                bool supportUnpublishedContent = false)
        {
            if (dataService == null)
            {
                dataService = new TestDataService();
            }
            if (contentService == null)
            {
                long longTotalRecs;
                int intTotalRecs;

                var allRecs = dataService.ContentService.GetLatestContentByXPath("//*[@isDoc]")
                    .Root
                    .Elements()
                    .Select(x => Mock.Of<IContent>(
                        m =>
                            m.Id == (int)x.Attribute("id") &&
                            m.ParentId == (int)x.Attribute("parentID") &&
                            m.Level == (int)x.Attribute("level") &&
                            m.CreatorId == 0 &&
                            m.SortOrder == (int)x.Attribute("sortOrder") &&
                            m.CreateDate == (DateTime)x.Attribute("createDate") &&
                            m.UpdateDate == (DateTime)x.Attribute("updateDate") &&
                            m.Name == (string)x.Attribute("nodeName") &&
                            m.Path == (string)x.Attribute("path") &&
                            m.Properties == new PropertyCollection() &&
                            m.ContentType == Mock.Of<IContentType>(mt =>
                                mt.Alias == x.Name.LocalName &&
                                mt.Id == (int)x.Attribute("nodeType") &&
                                mt.Icon == "test")))
                    .ToArray();


                contentService = Mock.Of<IContentService>(
                    x => x.GetPagedDescendants(
                        It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), out longTotalRecs, It.IsAny<string>(), It.IsAny<Direction>(), It.IsAny<bool>(), It.IsAny<string>())
                        ==
                        allRecs
                        && x.GetPagedDescendants(
                        It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), out longTotalRecs, It.IsAny<string>(), It.IsAny<Direction>(), It.IsAny<string>())
                        ==
                        allRecs
                        && x.GetPagedDescendants(
                        It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), out intTotalRecs, It.IsAny<string>(), It.IsAny<Direction>(), It.IsAny<string>())
                        ==
                        allRecs
                        && x.GetPagedDescendants(
                        It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), out longTotalRecs, It.IsAny<string>(), It.IsAny<Direction>(), It.IsAny<bool>(), It.IsAny<IQuery<IContent>>())
                        ==
                        allRecs);
            }
            if (userService == null)
            {
                userService = Mock.Of<IUserService>(x => x.GetProfileById(It.IsAny<int>()) == Mock.Of<IProfile>(p => p.Id == 0 && p.Name == "admin"));
            }
            if (mediaService == null)
            {
                long longTotalRecs;
                int intTotalRecs;

                var mediaXml = dataService.MediaService.GetLatestMediaByXpath("//node");
                var allRecs = mediaXml
                    .Root
                    .Elements()
                    .Select(x => Mock.Of<IMedia>(
                        m =>
                            m.Id == (int)x.Attribute("id") &&
                            m.ParentId == (int)x.Attribute("parentID") &&
                            m.Level == (int)x.Attribute("level") &&
                            m.CreatorId == 0 &&
                            m.SortOrder == (int)x.Attribute("sortOrder") &&
                            m.CreateDate == (DateTime)x.Attribute("createDate") &&
                            m.UpdateDate == (DateTime)x.Attribute("updateDate") &&
                            m.Name == (string)x.Attribute("nodeName") &&
                            m.Path == (string)x.Attribute("path") &&
                            m.Properties == new PropertyCollection() &&
                            m.ContentType == Mock.Of<IMediaType>(mt =>
                                mt.Alias == (string)x.Attribute("nodeTypeAlias") &&
                                mt.Id == (int)x.Attribute("nodeType"))))
                    .ToArray();

                // MOCK!
                var mediaServiceMock = new Mock<IMediaService>();

                mediaServiceMock
                    .Setup(x => x.GetPagedDescendants(
                            It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), out longTotalRecs, It.IsAny<string>(), It.IsAny<Direction>(), It.IsAny<bool>(), It.IsAny<string>())
                    ).Returns(() => allRecs);

                mediaServiceMock
                    .Setup(x => x.GetPagedDescendants(
                            It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), out longTotalRecs, It.IsAny<string>(), It.IsAny<Direction>(), It.IsAny<string>())
                    ).Returns(() => allRecs);

                mediaServiceMock
                   .Setup(x => x.GetPagedDescendants(
                           It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), out intTotalRecs, It.IsAny<string>(), It.IsAny<Direction>(), It.IsAny<string>())
                   ).Returns(() => allRecs);

                mediaServiceMock.Setup(service => service.GetPagedXmlEntries(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), out longTotalRecs))
                    .Returns(() => allRecs.Select(x => x.ToXml()));

                mediaService = mediaServiceMock.Object;

            }
            if (dataTypeService == null)
            {
                dataTypeService = Mock.Of<IDataTypeService>();
            }

            if (memberService == null)
            {
                memberService = Mock.Of<IMemberService>();
            }

            if (contentTypeService == null)
            {
                var contentTypeServiceMock = new Mock<IContentTypeService>();
                contentTypeServiceMock.Setup(x => x.GetAllMediaTypes())
                    .Returns(new List<IMediaType>()
                    {
                        new MediaType(-1) {Alias = "Folder", Name = "Folder", Id = 1031, Icon = "icon-folder"},
                        new MediaType(-1) {Alias = "Image", Name = "Image", Id = 1032, Icon = "icon-picture"}
                    });
                contentTypeService = contentTypeServiceMock.Object;
            }

            if (analyzer == null)
            {
                analyzer = new StandardAnalyzer(Version.LUCENE_29);
            }

            var indexSet = new IndexSet();
            var indexCriteria = indexSet.ToIndexCriteria(dataService, UmbracoContentIndexer.IndexFieldPolicies);

            var i = new UmbracoContentIndexer(indexCriteria,
                writer, 
                dataService,
                contentService,
                mediaService,
                dataTypeService,
                userService,
                contentTypeService,
                false)
            {
                SupportUnpublishedContent = supportUnpublishedContent
            };

            //i.IndexSecondsInterval = 1;

            i.IndexingError += IndexingError;

            return i;
        }

        public static UmbracoExamineSearcher GetUmbracoSearcher(IndexWriter writer, Analyzer analyzer = null)
        {
            if (analyzer == null)
            {
                analyzer = new StandardAnalyzer(Version.LUCENE_29);
            }
            return new UmbracoExamineSearcher(writer, analyzer);
        }

        public static LuceneSearcher GetLuceneSearcher(Directory luceneDir)
        {
            return new LuceneSearcher(luceneDir, new StandardAnalyzer(Version.LUCENE_29));
        }

        public static MultiIndexSearcher GetMultiSearcher(Directory pdfDir, Directory simpleDir, Directory conventionDir, Directory cwsDir)
        {
            var i = new MultiIndexSearcher(new[] { pdfDir, simpleDir, conventionDir, cwsDir }, new StandardAnalyzer(Version.LUCENE_29));
            return i;
        }


        internal static void IndexingError(object sender, IndexingErrorEventArgs e)
        {
            throw new ApplicationException(e.Message, e.InnerException);
        }


    }
}