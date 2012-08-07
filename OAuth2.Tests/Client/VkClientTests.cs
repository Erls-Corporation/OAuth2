﻿using System.Collections.Generic;
using System.Collections.Specialized;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using FluentAssertions;

namespace OAuth2.Tests.Client
{
    [TestFixture]
    public class VkClientTests
    {
        private const string content = "{\"response\":[{\"uid\":\"1\",\"first_name\":\"Павел\",\"last_name\":\"Дуров\",\"photo\":\"http:\\/\\/cs109.vkontakte.ru\\/u00001\\/c_df2abf56.jpg\"}]}";

        private VkClientDescendant descendant;
        private IRequestFactory factory;

        [SetUp]
        public void SetUp()
        {
            var client = Substitute.For<IRestClient>();
            var request = Substitute.For<IRestRequest>();

            factory = Substitute.For<IRequestFactory>();
            factory.NewClient().Returns(client);
            factory.NewRequest().Returns(request);

            var configurationManager = Substitute.For<IConfigurationManager>();

            var configurationSection = new OAuth2ConfigurationSection();

            configurationManager
                .GetConfigSection<OAuth2ConfigurationSection>("oauth2")
                .Returns(configurationSection);

            descendant = new VkClientDescendant(factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("http://oauth.vk.com");
            endpoint.Resource.Should().Be("/authorize");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://oauth.vk.com");
            endpoint.Resource.Should().Be("/access_token");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.vk.com");
            endpoint.Resource.Should().Be("/method/users.get");
        }

        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = descendant.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("1");
            info.FirstName.Should().Be("Павел");
            info.LastName.Should().Be("Дуров");
            info.Email.Should().BeNull();
            info.PhotoUri.Should().Be("http://cs109.vkontakte.ru/u00001/c_df2abf56.jpg");
        }

        [Test]
        public void Should_ReceiveUserId_WhenAccessTokenResponseReceived()
        {
            // arrange
            var request = Substitute.For<IRestRequest>();
            var response = Substitute.For<IRestResponse>();
            response.Content.Returns("{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}");

            var client = Substitute.For<IRestClient>();
            client.Execute(Arg.Is(request)).Returns(response);

            factory.NewClient().Returns(client);

            // act
            descendant.GetUserInfo(new NameValueCollection());

            // assert
            response.ReceivedCalls().Should().Contain(x => x.GetMethodInfo().Name == "get_Content");
        }

        [Test, Ignore]
        public void Should_AddExtraParameters_WhenOnGetUserInfoIsCalled()
        {
            // arrange
            var request = Substitute.For<IRestRequest>();
            request.Parameters.Returns(new List<Parameter>());

            var response = Substitute.For<IRestResponse>();
            response.Content.Returns(content);

            var client = Substitute.For<IRestClient>();
            client.Execute(Arg.Is(request)).Returns(response);

            var descendant = new VkClientDescendant(Substitute.For<IRequestFactory>(), Substitute.For<IClientConfiguration>());

            // act
            descendant.GetUserInfo(new NameValueCollection());

            // assert
            request.Received(1).AddParameter(Arg.Is("fields"), Arg.Is("uid,first_name,last_name,photo"));
        }

        private class VkClientDescendant : VkClient
        {
            public VkClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration)
            {
            }

            public Endpoint GetAccessCodeServiceEndpoint()
            {
                return AccessCodeServiceEndpoint;
            }

            public Endpoint GetAccessTokenServiceEndpoint()
            {
                return AccessTokenServiceEndpoint;
            }

            public Endpoint GetUserInfoServiceEndpoint()
            {
                return UserInfoServiceEndpoint;
            }

            public new UserInfo ParseUserInfo(string content)
            {
                return base.ParseUserInfo(content);
            }
        }
    }
}