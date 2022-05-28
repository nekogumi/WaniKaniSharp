using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekogumi.WaniKani.Services
{

    public enum ObjectType
    {
        Radical,
        Kanji,
        Vocabulary,
        Collection,
        Report,

        Assignment,
        //LevelProgression,
        level_progression,
        Reset,
        Review,
        //ReviewStatistic,
        review_statistic,
        //StudyMaterial,
        study_material,
        User,
        //VoiceActor,
        voice_actor,
        //SpacedRepetitionSystem,
        spaced_repetition_system,
    }

    public enum SubscriptionType
    {
        Free,
        Recurring,
        Lifetime,
    }

    public enum SubjectType
    {
        Radical = ObjectType.Radical,
        Kanji = ObjectType.Kanji,
        Vocabulary = ObjectType.Vocabulary,
    }

    /// <summary>
    /// Response base class.
    /// </summary>
    /// <param name="object">
    /// The kind of object returned. See the object types (<see cref="ObjectType"/>) for all the kinds.
    /// </param>
    /// <param name="url">The URL of the request.
    /// <para>For collections, that will contain all the filters and options you've passed to the API.</para>
    /// <para>Resources have a single URL and don't need to be filtered, so the URL will be the same 
    /// in both resource and collection responses.</para></param>
    /// <param name="data_updated_at">
    /// <para>For collections, this is the timestamp of the most recently updated resource in the specified scope and is not limited by pagination.</para>
    /// <para>For a resource, then this is the last time that particular resource was updated.</para>
    /// </param>
    [DebuggerDisplay("{Object}")]
    public record Response<TData>(
                ObjectType Object,
                string Url,
                DateTime? DataUpdatedAt,
                TData Data);

    public record Resource<TData>(
        long Id,
        ObjectType Object,
        string Url,
        DateTime? DataUpdatedAt,
        TData Data
        );




    //[DebuggerDisplay("{@object} {id}")]
    //public abstract record Resource(ObjectType @object, string url, DateTime? data_updated_at, long id)
    //    : Response(@object, url, data_updated_at)
    //{
    //    //public abstract IResourceData Data { get; }
    //}

    //public interface IResourceData
    //{
    //    DateTime created_at { get; }
    //}

    //public record Resource<TData>(ObjectType @object, string url, DateTime? data_updated_at, long id, TData data) 
    //    : Resource(@object, url, data_updated_at, id)
    //    //where TData : IResourceData
    //{
    //    //public override IResourceData Data => data;
    //}

    [DebuggerDisplay("{Object} {Count}")]
    public record ResponseCollection<TData>(
                ObjectType Object,
                string Url,
                DateTime? DataUpdatedAt,
                Pages Pages,
                int TotalCount,
                Resource<TData>[] Data)
        : Response<Resource<TData>[]>(Object, Url, DataUpdatedAt, Data)
        //, IReadOnlyList<TData>
        //where TData : IResourceData
    {
        public int Count => Data.Length;
 
        ///// <summary>
        ///// This is going to be the resources returned by the specified scope.</para>
        ///// </summary>
        //public IReadOnlyList<Resource<TData>> data { get; }

        //public Resource<TData> this[int index] => data[index];
        //public IEnumerator<Resource<TData>> GetEnumerator() => Data.GetEnumerator();
        //IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="NextUrl">The URL of the next page of results.If there are no more results, the value is null.</param>
    /// <param name="PreviousUrl">The URL of the previous page of results. If there are no results at all or no previous page to go to, the value is null.</param>
    /// <param name="PerPage">Maximum number of resources delivered for this collection.</param>
    public record Pages(string? NextUrl, string? PreviousUrl, int PerPage);

}
