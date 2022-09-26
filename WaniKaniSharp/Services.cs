// AUTO GENERATED FILE -- DO NOT EDIT
namespace Nekogumi.WaniKani.Services;
partial class WaniKaniConnection
{
    public Task<ResponseCollection<AssignmentData>> QueryAssignmentsAsync(
            DateTime? available_before = null,
            bool? burned = null,
            bool? hidden = null,
            IEnumerable<long>? ids = null,
            bool? immediately_available_for_lessons = null,
            bool? immediately_available_for_review = null,
            bool? in_review = null,
            IEnumerable<int>? levels = null,
            IEnumerable<long>? srs_stages = null,
            bool? started = null,
            IEnumerable<long>? subject_ids = null,
            IEnumerable<string>? subject_types = null,
            bool? unlocked = null,
            DateTime? updated_after = null,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => QueryCollectionAsync<AssignmentData>("assignments", cacheStrategy, cancellationToken
            , ("available_before", available_before)
            , ("burned", burned)
            , ("hidden", hidden)
            , ("ids", ids)
            , ("immediately_available_for_lessons", immediately_available_for_lessons)
            , ("immediately_available_for_review", immediately_available_for_review)
            , ("in_review", in_review)
            , ("levels", levels)
            , ("srs_stages", srs_stages)
            , ("started", started)
            , ("subject_ids", subject_ids)
            , ("subject_types", subject_types)
            , ("unlocked", unlocked)
            , ("updated_after", updated_after)
            );
    
    public Task<Response<AssignmentData>> QueryAssignmentAsync(
            long id,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => GetJsonAsync<Response<AssignmentData>>($"assignments/{id}", cacheStrategy, cancellationToken);
    
    public Task<ResponseCollection<LevelProgressionData>> QueryLevelProgressionsAsync(
            DateTime? updated_after = null,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => QueryCollectionAsync<LevelProgressionData>("level_progressions", cacheStrategy, cancellationToken
            , ("updated_after", updated_after)
            );
    
    public Task<Response<LevelProgressionData>> QueryLevelProgressionAsync(
            long id,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => GetJsonAsync<Response<LevelProgressionData>>($"level_progressions/{id}", cacheStrategy, cancellationToken);
    
    public Task<ResponseCollection<ResetData>> QueryResetsAsync(
            DateTime? updated_after = null,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => QueryCollectionAsync<ResetData>("resets", cacheStrategy, cancellationToken
            , ("updated_after", updated_after)
            );
    
    public Task<Response<ResetData>> QueryResetAsync(
            long id,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => GetJsonAsync<Response<ResetData>>($"resets/{id}", cacheStrategy, cancellationToken);
    
    public Task<ResponseCollection<ReviewData>> QueryReviewsAsync(
            IEnumerable<long>? ids = null,
            IEnumerable<long>? subject_ids = null,
            DateTime? updated_after = null,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => QueryCollectionAsync<ReviewData>("reviews", cacheStrategy, cancellationToken
            , ("ids", ids)
            , ("subject_ids", subject_ids)
            , ("updated_after", updated_after)
            );
    
    public Task<Response<ReviewData>> QueryReviewAsync(
            long id,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => GetJsonAsync<Response<ReviewData>>($"reviews/{id}", cacheStrategy, cancellationToken);
    
    public Task<ResponseCollection<ReviewStatisticData>> QueryReviewStatisticsAsync(
            IEnumerable<long>? ids = null,
            long? percentages_greater_than = null,
            long? percentages_less_than = null,
            IEnumerable<long>? subject_ids = null,
            IEnumerable<string>? subject_types = null,
            DateTime? updated_after = null,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => QueryCollectionAsync<ReviewStatisticData>("review_statistics", cacheStrategy, cancellationToken
            , ("ids", ids)
            , ("percentages_greater_than", percentages_greater_than)
            , ("percentages_less_than", percentages_less_than)
            , ("subject_ids", subject_ids)
            , ("subject_types", subject_types)
            , ("updated_after", updated_after)
            );
    
    public Task<Response<ReviewStatisticData>> QueryReviewStatisticAsync(
            long id,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => GetJsonAsync<Response<ReviewStatisticData>>($"review_statistics/{id}", cacheStrategy, cancellationToken);
    
    public Task<ResponseCollection<SpacedRepetitionSystemData>> QuerySpacedRepetitionSystemsAsync(
            DateTime? updated_after = null,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => QueryCollectionAsync<SpacedRepetitionSystemData>("spaced_repetition_systems", cacheStrategy, cancellationToken
            , ("updated_after", updated_after)
            );
    
    public Task<Response<SpacedRepetitionSystemData>> QuerySpacedRepetitionSystemAsync(
            long id,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => GetJsonAsync<Response<SpacedRepetitionSystemData>>($"spaced_repetition_systems/{id}", cacheStrategy, cancellationToken);
    
    public Task<ResponseCollection<StudyMaterialData>> QueryStudyMaterialsAsync(
            IEnumerable<long>? ids = null,
            IEnumerable<long>? subject_ids = null,
            IEnumerable<string>? subject_types = null,
            DateTime? updated_after = null,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => QueryCollectionAsync<StudyMaterialData>("study_materials", cacheStrategy, cancellationToken
            , ("ids", ids)
            , ("subject_ids", subject_ids)
            , ("subject_types", subject_types)
            , ("updated_after", updated_after)
            );
    
    public Task<Response<StudyMaterialData>> QueryStudyMaterialAsync(
            long id,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => GetJsonAsync<Response<StudyMaterialData>>($"study_materials/{id}", cacheStrategy, cancellationToken);
    
    public Task<ResponseCollection<SubjectData>> QuerySubjectsAsync(
            IEnumerable<string>? types = null,
            IEnumerable<string>? slugs = null,
            IEnumerable<int>? levels = null,
            bool? hidden = null,
            DateTime? updated_after = null,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => QueryCollectionAsync<SubjectData>("subjects", cacheStrategy, cancellationToken
            , ("types", types)
            , ("slugs", slugs)
            , ("levels", levels)
            , ("hidden", hidden)
            , ("updated_after", updated_after)
            );
    
    public Task<Response<SubjectData>> QuerySubjectAsync(
            long id,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => GetJsonAsync<Response<SubjectData>>($"subjects/{id}", cacheStrategy, cancellationToken);
    
    public Task<Response<SummaryData>> QuerySummaryAsync(
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => GetJsonAsync<Response<SummaryData>>("summary", cacheStrategy, cancellationToken);
    
    public Task<Response<UserData>> QueryUserAsync(
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => GetJsonAsync<Response<UserData>>("user", cacheStrategy, cancellationToken);
    
    public Task<ResponseCollection<VoiceActorData>> QueryVoiceActorsAsync(
            DateTime? updated_after = null,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => QueryCollectionAsync<VoiceActorData>("voice_actors", cacheStrategy, cancellationToken
            , ("updated_after", updated_after)
            );
    
    public Task<Response<VoiceActorData>> QueryVoiceActorAsync(
            long id,
            CacheStrategy cacheStrategy = CacheStrategy.Cache,
            CancellationToken cancellationToken = default)
        => GetJsonAsync<Response<VoiceActorData>>($"voice_actors/{id}", cacheStrategy, cancellationToken);
    
}
