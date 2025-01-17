﻿using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using MiniBlog;
using MiniBlog.Model;
using MiniBlog.Repositories;
using MiniBlog.Stores;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace MiniBlogTest.ControllerTest
{
    [Collection("IntegrationTest")]
    public class ArticleControllerTest : TestBase
    {
        public ArticleControllerTest(CustomWebApplicationFactory<Startup> factory)
            : base(factory)
        {
        }

        [Fact]
        public async void Should_get_all_Article()
        {
            var mock = new Mock<IArticleRepository>();
            mock.Setup(repository => repository.GetArticles()).Returns(Task.FromResult(new List<Article>
            {
                new Article(null, "Happy new year", "Happy 2021 new year"),
                new Article(null, "Happy Halloween", "Halloween is coming"),
            }));
            var client = GetClient(null, null, mock.Object);
            var response = await client.GetAsync("/article");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var articles = JsonConvert.DeserializeObject<List<Article>>(body);
            Assert.Equal(2, articles.Count);
        }

        [Fact]
        public async void Should_create_article_fail_when_ArticleStore_unavailable()
        {
            var client = GetClient();
            string userNameWhoWillAdd = "Tom";
            string articleContent = "What a good day today!";
            string articleTitle = "Good day";
            Article article = new Article(userNameWhoWillAdd, articleTitle, articleContent);

            var httpContent = JsonConvert.SerializeObject(article);
            StringContent content = new StringContent(httpContent, Encoding.UTF8, MediaTypeNames.Application.Json);
            var response = await client.PostAsync("/article", content);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async void Should_create_article_and_register_user_correct()
        {
            var mock = new Mock<IArticleRepository>();
            mock.Setup(x => x.GetArticles()).Returns(Task.FromResult(new List<Article>
            {
                new Article("Kevin", "Happy new year", "Happy 2021 new year"),
                new Article("Tom", "What a good day today!", "Good day"),
            }));
            mock.Setup(x => x.CreateArticle(It.IsAny<Article>())).Returns(Task.FromResult(
                new Article(null, "Happy new year", "Happy 2021 new year")));

            var mock2 = new Mock<IUserRepository>();
            mock2.Setup(x => x.GetUsers()).Returns(Task.FromResult(new List<User>
            {
                new User("Tom", "123@qq.com"),
            }));
            mock2.Setup(x => x.CreateUser(It.IsAny<User>())).Returns(Task.FromResult(new User("Tom", "123@qq.com")));

            var client = GetClient(null, null, mock.Object, mock2.Object);

            string userNameWhoWillAdd = "Tom";
            string articleContent = "Good day";
            string articleTitle = "What a good day today!";
            Article article = new Article(userNameWhoWillAdd, articleTitle, articleContent);

            var httpContent = JsonConvert.SerializeObject(article);
            StringContent content = new StringContent(httpContent, Encoding.UTF8, MediaTypeNames.Application.Json);
            var createArticleResponse = await client.PostAsync("/article", content);

            // It fail, please help
            Assert.Equal(HttpStatusCode.Created, createArticleResponse.StatusCode);

            var articleResponse = await client.GetAsync("/article");
            var body = await articleResponse.Content.ReadAsStringAsync();
            var articles = JsonConvert.DeserializeObject<List<Article>>(body);
            Assert.Equal(2, articles.Count);
            Assert.Equal(articleTitle, articles[1].Title);
            Assert.Equal(articleContent, articles[1].Content);
            Assert.Equal(userNameWhoWillAdd, articles[1].UserName);

            //var userResponse = await client.GetAsync("/user");
            //var usersJson = await userResponse.Content.ReadAsStringAsync();
            //var users = JsonConvert.DeserializeObject<List<User>>(usersJson);

            //Assert.True(users.Count == 1);
            //Assert.Equal("Tom", users[0].Name);
            //Assert.Equal("123@qq.com", users[0].Email);
        }
    }
}
