﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Cocona;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Polly;
using Polly.Retry;

namespace WatchDotNetGitHubRepos
{
    class Program
    {
        private const string FeedSiteUrl = "https://github.com/mayuki/WatchDotNetGitHubRepos";
        private const string FeedBaseTitle = ".NET related GitHub";

        static void Main(string[] args)
        {
            CoconaLiteApp.Run<Program>(args);
        }

        public async Task Releases(string outputDir, [Argument] string[] targets)
        {
            var now = DateTimeOffset.UtcNow;
            var beginOfToday = now.UtcDateTime.Date;
            var beginOfYesterday = now.UtcDateTime.Date.AddDays(-1);

            var allEntries = new List<XElement>();
            foreach (var target in targets)
            {
                var parts = target.Split('/');
                var (owner, repository) = (parts[0], parts[1]);

                Console.WriteLine($"Fetching Releases of {owner}/{repository} ({beginOfYesterday})");
                var releasesTask = Queries.GetReleases(owner, repository);
                var releases = (await releasesTask).Repository.Releases.Edges.Select(x => x.Node)
                    .Where(x => beginOfYesterday <= x.PublishedAt && x.PublishedAt < beginOfToday)
                    .ToArray(); // Yesterday
                var title = $"{owner}/{repository} - Releases";
                var repositoryUrl = $"https://github.com/{owner}/{repository}";
                var entries = Array.Empty<XElement>();
                if (releases.Any())
                {
                    var releasesHtml = AtomFeedFormatter.Releases(releases);

                    entries = new XElement[]
                    {
                        AtomFeedFormatter.CreateEntry(title, repositoryUrl, beginOfYesterday, releasesHtml)
                    };
                }
                else
                {
                    Console.WriteLine($"The repository has no changes.");
                }

                var feed = AtomFeedFormatter.CreateFeed(title, repositoryUrl, entries);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var outputPath = Path.Combine(outputDir, $"{owner}_{repository}.atom");
                Console.WriteLine($"Write {outputPath}");
                File.WriteAllText(outputPath, feed.ToString(), Encoding.UTF8);

                allEntries.AddRange(entries);
            }

            // Consolidated
            {
                var feed = AtomFeedFormatter.CreateFeed($"{FeedBaseTitle} Releases", FeedSiteUrl, allEntries);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var outputPath = Path.Combine(outputDir, $"releases.atom");
                Console.WriteLine($"Write {outputPath}");
                File.WriteAllText(outputPath, feed.ToString(), Encoding.UTF8);
            }
        }

        public async Task IssuesAndPullRequests(string outputDir, [Argument]string[] targets)
        {
            var now = DateTimeOffset.UtcNow;
            var beginOfToday = now.UtcDateTime.Date;
            var beginOfYesterday = now.UtcDateTime.Date.AddDays(-1);

            static bool IsUpdateDependencies(string title)
                => title.Contains("Update dependencies from");

            var allEntries = new List<XElement>();
            foreach (var target in targets)
            {
                var parts = target.Split('/');
                var (owner, repository) = (parts[0], parts[1]);

                Console.WriteLine($"Fetching Issues / PullRequests of {owner}/{repository} ({beginOfYesterday})");
                var issuesAndPRs = await Queries.GetIssuesAndPullRequestsAsync(owner, repository);

                var createdIssues = issuesAndPRs.Repository.CreatedIssues.Edges.Select(x => x.Node).Where(x => beginOfYesterday <= x.CreatedAt && x.CreatedAt < beginOfToday).ToArray(); // Yesterday
                var updatedIssues = issuesAndPRs.Repository.UpdatedIssues.Edges.Select(x => x.Node).Where(x => beginOfYesterday <= x.UpdatedAt && !createdIssues.Contains(x)).ToArray(); // Yesterday and Today
                var closedIssues = issuesAndPRs.Repository.ClosedIssues.Edges.Select(x => x.Node).Where(x => beginOfYesterday <= x.ClosedAt && x.ClosedAt < beginOfToday).ToArray(); // Yesterday
                var createdPRs = issuesAndPRs.Repository.CreatedPullRequests.Edges.Select(x => x.Node).Where(x => !IsUpdateDependencies(x.Title) && beginOfYesterday <= x.CreatedAt && x.CreatedAt < beginOfToday).ToArray(); // Yesterday
                var updatedPRs = issuesAndPRs.Repository.UpdatedPullRequests.Edges.Select(x => x.Node).Where(x => !IsUpdateDependencies(x.Title) && beginOfYesterday <= x.UpdatedAt && !createdPRs.Contains(x)).ToArray(); // Yesterday and Today
                var mergedPRs = issuesAndPRs.Repository.MergedPullRequests.Edges.Select(x => x.Node).Where(x => !IsUpdateDependencies(x.Title) && beginOfYesterday <= x.MergedAt && x.MergedAt < beginOfToday).ToArray(); // Yesterday

                var title = $"{owner}/{repository} - Issues & Pull Requests";
                var repositoryUrl = $"https://github.com/{owner}/{repository}";
                var entries = Array.Empty<XElement>();
                if (updatedPRs.Any() ||
                    createdPRs.Any() ||
                    mergedPRs.Any() ||
                    updatedIssues.Any() ||
                    createdIssues.Any() ||
                    closedIssues.Any())
                {
                    var prs = AtomFeedFormatter.PullRequests(updatedPRs, createdPRs, mergedPRs);
                    var issues = AtomFeedFormatter.Issues(updatedIssues, createdIssues, closedIssues);

                    entries = new XElement[]
                    {
                        AtomFeedFormatter.CreateEntry(title, repositoryUrl, beginOfYesterday, issues.ToString() + prs.ToString())
                    };
                }
                else
                {
                    Console.WriteLine($"The repository has no changes.");
                }

                var feed = AtomFeedFormatter.CreateFeed(title, repositoryUrl, entries);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var outputPath = Path.Combine(outputDir, $"{owner}_{repository}.atom");
                Console.WriteLine($"Write {outputPath}");
                File.WriteAllText(outputPath, feed.ToString(), Encoding.UTF8);

                allEntries.AddRange(entries);
            }

            // Consolidated
            {
                var feed = AtomFeedFormatter.CreateFeed($"{FeedBaseTitle} Issues & Pull Requests", FeedSiteUrl, allEntries);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var outputPath = Path.Combine(outputDir, $"issues-and-pull-requests.atom");
                Console.WriteLine($"Write {outputPath}");
                File.WriteAllText(outputPath, feed.ToString(), Encoding.UTF8);
            }
        }
    }

    static class Helper
    {
        public static string GetAccessToken() => Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    }

    class AtomFeedFormatter
    {
        public static XNamespace AtomNs = "http://www.w3.org/2005/Atom";

        public static XElement CreateFeed(string title, string repositoryUrl, IEnumerable<XElement> entries)
        {
            return new XElement(AtomNs + "feed",
                new XElement(AtomNs + "title", title),
                new XElement(AtomNs + "link", new XAttribute("href", repositoryUrl)),
                new XElement(AtomNs + "updated", DateTimeOffset.UtcNow.ToString("s") + "Z"),
                new XElement(AtomNs + "id", repositoryUrl),
                entries
            );
        }

        public static XElement CreateEntry(string title, string repositoryUrl, DateTimeOffset entryDate, string content)
        {
            return new XElement(AtomNs + "entry",
                new XElement(AtomNs + "title", $"{title} - {entryDate:yyyy-MM-dd}"),
                new XElement(AtomNs + "id", $"{repositoryUrl}#{entryDate:yyyy-MM-dd}"),
                new XElement(AtomNs + "link", new XAttribute("href", repositoryUrl)),
                new XElement(AtomNs + "updated", DateTimeOffset.UtcNow.ToString("s") + "Z"),
                new XElement(AtomNs + "content", new XAttribute("type", "html"), content)
            );
        }

        public static string Releases(IReadOnlyList<Release> published)
        {
            if (!published.Any()) return "";

            static string Escape(string value)
                => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

            return $@"
                <div>
                    {string.Concat(published.Select(x => @$"
                        <article>
                            <h2><a href=""{Escape(x.Url)}"">{Escape(string.IsNullOrWhiteSpace(x.Name) ? x.TagName : x.Name)}</a></h2>
                            <div>
                                {x.DescriptionHTML}
                            </div>
                        </article>
                    "))}
                </div>
            ";
        }

        public static XElement Issues(IReadOnlyList<Issue> updated, IReadOnlyList<Issue> created, IReadOnlyList<Issue> closed)
        {
            static IReadOnlyList<XElement> CreateFromIssues(string title, IReadOnlyList<Issue> issues)
            {
                if (!issues.Any()) return Array.Empty<XElement>();

                return new []
                {
                    new XElement("h3", title),
                    new XElement("ul",
                        issues.Select(x =>
                            new XElement("li",
                                new XElement("div",
                                    new XElement("a", new XAttribute("href", x.Url), $"{x.Title} #{x.Number}")
                                ),
                                new XElement("div",
                                    $"{string.Join(", ", (x.Labels?.Edges.Select(y => y.Node.Name) ?? Array.Empty<string>()).Concat(x.Milestone != null ? new[] {x.Milestone.Title} : Array.Empty<string>()))}"
                                )
                            )
                        )
                    )
                };
            }

            if (!updated.Any() && !created.Any() && !closed.Any())
            {
                return new XElement("div");
            }

            return new XElement("section",
                new XElement("h2", "Issues"),
                new [] { ("Created", created), ("Closed", closed), ("Updated", updated) }.SelectMany(x => CreateFromIssues(x.Item1, x.Item2.ToArray()))
            );
        }
        public static XElement PullRequests(IReadOnlyList<PullRequest> updated, IReadOnlyList<PullRequest> created, IReadOnlyList<PullRequest> merged)
        {
            static IReadOnlyList<XElement> CreateFromPRs(string title, IReadOnlyList<PullRequest> pullRequests)
            {
                if (!pullRequests.Any()) return Array.Empty<XElement>();

                return new[]
                {
                    new XElement("h3", title),
                    new XElement("ul",
                        pullRequests.Select(x =>
                            new XElement("li",
                                new XElement("div",
                                    new XElement("a", new XAttribute("href", x.Url), $"{x.Title} #{x.Number}")
                                ),
                                new XElement("div",
                                    $"{string.Join(", ", (x.Labels?.Edges.Select(y => y.Node.Name) ?? Array.Empty<string>()).Concat(x.Milestone != null ? new[] {x.Milestone.Title} : Array.Empty<string>()))}"
                                )
                            )
                        )
                    )
                };
            }

            if (!updated.Any() && !created.Any() && !merged.Any())
            {
                return new XElement("div");
            }

            return new XElement("section",
                new XElement("h2", "PullRequests"),
                new[] { ("Created", created), ("Merged", merged), ("Updated", updated) }.SelectMany(x => CreateFromPRs(x.Item1, x.Item2.ToArray()))
            );
        }
    }

    class Queries
    {
        // https://docs.github.com/en/graphql/overview/explorer
        const string Query = @"
            fragment PullRequestEdges on PullRequestConnection {
              edges {
                node {
                  id
                  title
                  url
                  number
                  createdAt
                  updatedAt
                  mergedAt
                  labels(first: 10) {
                    edges {
                      node {
                        name
                      }
                    }
                  }
                  milestone {
                    title
                  }
                }
              }
            }
            fragment IssueEdges on IssueConnection {
              edges {
                node {
                  title
                  url
                  number
                  createdAt
                  updatedAt
                  closedAt
                  milestone {
                    title
                  }
                }
              }
            }
            fragment ReleaseEdges on ReleaseConnection {
              edges {
                node {
                  id
                  url
                  name
                  tagName
                  publishedAt
                  createdAt
                  updatedAt
                  description
                  descriptionHTML
                  isPrerelease
                }
              }
            }

            query GetReleases($repository: String!, $owner: String!, $count: Int) {
              repository(name: $repository, owner: $owner) {
                releases(first: $count, orderBy: {field: CREATED_AT, direction: DESC}) {
                  ...ReleaseEdges
                }
              }
            }

            query GetIssuesAndPullRequests($repository: String!, $owner: String!, $count: Int) {
              repository(name: $repository, owner: $owner) {
                updatedIssues: issues(first: $count, orderBy: {field: UPDATED_AT, direction: DESC}, states: OPEN) {
                  ...IssueEdges
                }
                createdIssues: issues(first: $count, orderBy: {field: CREATED_AT, direction: DESC}, states: OPEN) {
                  ...IssueEdges
                }
                closedIssues: issues(first: $count, orderBy: {field: UPDATED_AT, direction: DESC}, states: CLOSED) {
                  ...IssueEdges
                }
                updatedPullRequests: pullRequests(first: $count, orderBy: {field: UPDATED_AT, direction: DESC}, states: OPEN) {
                  ...PullRequestEdges
                }
                createdPullRequests: pullRequests(first: $count, orderBy: {field: CREATED_AT, direction: DESC}, states: OPEN) {
                  ...PullRequestEdges
                }
                mergedPullRequests: pullRequests(first: $count, orderBy: {field: UPDATED_AT, direction: DESC}, states: MERGED) {
                  ...PullRequestEdges
                }
              }
            }

            query GetIssues($repository: String!, $owner: String!, $orderField: IssueOrderField!, $issueState: [IssueState!], $count: Int) {
              repository(name: $repository, owner: $owner) {
                issues(first: $count, orderBy: {field: $orderField, direction: DESC}, states: $issueState) {
                  ...IssueEdges
                }
              }
            }
            query GetPullRequests($repository: String!, $owner: String!, $orderField: IssueOrderField!, $prState: [PullRequestState!], $count: Int) {
              repository(name: $repository, owner: $owner) {
                pullRequests(first: $count, orderBy: {field: $orderField, direction: DESC}, states: $prState) {
                  ...PullRequestEdges
                }
              }
            }
        ";

        private const int MaxFetchIssueCount = 100;
        private const int MaxFetchReleaseCount = 10;
        private const int MaxRetryCount = 10;

        private static AsyncRetryPolicy CreateRetryPolicy()
        {
            return Policy.Handle<GraphQLHttpRequestException>()
                .WaitAndRetryAsync(MaxRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)));
        }

        public static async Task<RepositoryQueryResponse> GetReleases(string owner, string repository)
        {
            var graphQLClient = new GraphQLHttpClient("https://api.github.com/graphql", new SystemTextJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Helper.GetAccessToken()}");

            var response = await CreateRetryPolicy().ExecuteAsync(() => graphQLClient.SendQueryAsync<RepositoryQueryResponse>(new GraphQLRequest
            {
                Query = Query,
                OperationName = "GetReleases",
                Variables = new
                {
                    owner = owner,
                    repository = repository,
                    count = MaxFetchReleaseCount,
                }
            }));

            return response.Data;
        }

        public static async Task<GetIssuesAndPullRequestsRepositoryQueryResponse> GetIssuesAndPullRequestsAsync(string owner, string repository)
        {
            var graphQLClient = new GraphQLHttpClient("https://api.github.com/graphql", new SystemTextJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Helper.GetAccessToken()}");

            var response = await CreateRetryPolicy().ExecuteAsync(() => graphQLClient.SendQueryAsync<GetIssuesAndPullRequestsRepositoryQueryResponse>(new GraphQLRequest
            {
                Query = Query,
                OperationName = "GetIssuesAndPullRequests",
                Variables = new
                {
                    owner = owner,
                    repository = repository,
                    count = MaxFetchIssueCount,
                }
            }));

            return response.Data;
        }

        public static async Task<RepositoryQueryResponse> GetPullRequestsAsync(string owner, string repository, IssueOrderField orderField, PullRequestState prState)
        {
            var graphQLClient = new GraphQLHttpClient("https://api.github.com/graphql", new SystemTextJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Helper.GetAccessToken()}");

            var response = await CreateRetryPolicy().ExecuteAsync(() => graphQLClient.SendQueryAsync<RepositoryQueryResponse>(new GraphQLRequest
            {
                Query = Query,
                OperationName = "GetPullRequests",
                Variables = new
                {
                    owner = owner,
                    repository = repository,
                    orderField = orderField,
                    prState = prState,
                    count = MaxFetchIssueCount,
                }
            }));

            return response.Data;
        }
        public static async Task<RepositoryQueryResponse> GetIssuesAsync(string owner, string repository, IssueOrderField orderField, IssueState issueState)
        {
            var graphQLClient = new GraphQLHttpClient("https://api.github.com/graphql", new SystemTextJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Helper.GetAccessToken()}");

            var response = await CreateRetryPolicy().ExecuteAsync(() => graphQLClient.SendQueryAsync<RepositoryQueryResponse>(new GraphQLRequest
            {
                Query = Query,
                OperationName = "GetIssues",
                Variables = new
                {
                    owner = owner,
                    repository = repository,
                    orderField = orderField,
                    issueState = issueState,
                    count = MaxFetchIssueCount,
                }
            }));

            return response.Data;
        }
    }
}
