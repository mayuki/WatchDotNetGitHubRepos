using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchDotNetGitHubRepos
{
    /// <summary>
    /// The possible states of an issue.
    /// </summary>
    public enum IssueState
    {
        /// <summary>
        /// An issue that has been closed
        /// </summary>
        CLOSED,
        /// <summary>
        /// An issue that is still open
        /// </summary>
        OPEN
    }
    /// <summary>
    /// The possible states of a pull request.
    /// </summary>
    public enum PullRequestState
    {
        /// <summary>
        /// A pull request that has been closed without being merged.
        /// </summary>
        CLOSED,
        /// <summary>
        /// A pull request that has been closed by being merged.
        /// </summary>
        MERGED,
        /// <summary>
        /// A pull request that is still open.
        /// </summary>
        OPEN
    }
    /// <summary>
    /// Properties by which issue connections can be ordered.
    /// </summary>
    public enum IssueOrderField
    {
        /// <summary>
        /// Order issues by comment count
        /// </summary>
        COMMENTS,
        /// <summary>
        /// Order issues by creation time
        /// </summary>
        CREATED_AT,
        /// <summary>
        /// Order issues by update time
        /// </summary>
        UPDATED_AT
    }

    public class EdgeNode<T>
    {
        public T Node { get; set; }
    }
    public class EdgesType<T>
    {
        public EdgeNode<T>[] Edges { get; set; }
    }


    public class RepositoryQueryResponse
    {
        public Repository Repository { get; set; }
    }

    public class Repository
    {
        public EdgesType<PullRequest> PullRequests { get; set; }
        public EdgesType<Issue> Issues { get; set; }
        public EdgesType<Release> Releases { get; set; }
    }

    public class Release : IEquatable<Release>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public bool IsPrerelease { get; set; }
        public string Description { get; set; }
        public string DescriptionHTML { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset PublishedAt { get; set; }

        public bool Equals(Release other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Release)obj);
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }
    }

    public class Issue : IEquatable<Issue>
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public int Number { get; set; }
        public EdgesType<Label> Labels { get; set; }
        public Milestone Milestone { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }

        public override string ToString()
            => $"{Title} #{Number}";

        public bool Equals(Issue other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Issue)obj);
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }
    }


    public class PullRequest : IEquatable<PullRequest>
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public int Number { get; set; }
        public EdgesType<Label> Labels { get; set; }
        public Milestone Milestone { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? MergedAt { get; set; }

        public override string ToString()
            => $"{Title} #{Number}";

        public bool Equals(PullRequest other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PullRequest)obj);
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }
    }

    public class Milestone
    {
        public string Title { get; set; }
        public override string ToString()
            => $"{Title}";
    }

    public class Label
    {
        public string Name { get; set; }
        public override string ToString()
            => $"{Name}";
    }
}
