// AUTO GENERATED FILE -- DO NOT EDIT
using System;

namespace Nekogumi.WaniKaniSharp
{
    #region assignment-data-structure

    /// <summary></summary>
    /// <param name="AvailableAt">Timestamp when the related subject will be available in the user's review queue.</param>
    /// <param name="BurnedAt">Timestamp when the user reaches SRS stage 9 the first time.</param>
    /// <param name="CreatedAt">Timestamp when the assignment was created.</param>
    /// <param name="Hidden">Indicates if the associated subject has been hidden, preventing it from appearing in lessons or reviews.</param>
    /// <param name="PassedAt">Timestamp when the user reaches SRS stage 5 for the first time.</param>
    /// <param name="ResurrectedAt">Timestamp when the subject is resurrected and placed back in the user's review queue.</param>
    /// <param name="SrsStage">The current SRS stage interval. The interval range is determined by the related subject's spaced repetition system.</param>
    /// <param name="StartedAt">Timestamp when the user completes the lesson for the related subject.</param>
    /// <param name="SubjectId">Unique identifier of the associated subject.</param>
    /// <param name="SubjectType">The type of the associated subject, one of: kanji, radical, or vocabulary.</param>
    /// <param name="UnlockedAt">The timestamp when the related subject has its prerequisites satisfied and is made available in lessons. Prerequisites are:The subject components have reached SRS stage 5 once (they have been �passed�).The user's level is equal to or greater than the level of the assignment�s subject.</param>
    public record AssignmentData(
        DateTime? AvailableAt,
        DateTime? BurnedAt,
        DateTime CreatedAt,
        bool Hidden,
        DateTime? PassedAt,
        DateTime? ResurrectedAt,
        int SrsStage,
        DateTime? StartedAt,
        int SubjectId,
        SubjectType SubjectType,
        DateTime? UnlockedAt);
    
    #endregion

    #region level-progression-data-structure

    /// <summary></summary>
    /// <param name="AbandonedAt">Timestamp when the user abandons the level. This is primary used when the user initiates a reset.</param>
    /// <param name="CompletedAt">Timestamp when the user burns 100% of the assignments belonging to the associated subject's level.</param>
    /// <param name="CreatedAt">Timestamp when the level progression is created</param>
    /// <param name="Level">The level of the progression, with possible values from 1 to 60.</param>
    /// <param name="PassedAt">Timestamp when the user passes at least 90% of the assignments with a type of kanji belonging to the associated subject's level.</param>
    /// <param name="StartedAt">Timestamp when the user starts their first lesson of a subject belonging to the level.</param>
    /// <param name="UnlockedAt">Timestamp when the user can access lessons and reviews for the level.</param>
    public record LevelProgressionData(
        DateTime? AbandonedAt,
        DateTime? CompletedAt,
        DateTime CreatedAt,
        int Level,
        DateTime? PassedAt,
        DateTime? StartedAt,
        DateTime? UnlockedAt);
    
    #endregion

    #region reset-data-structure

    /// <summary></summary>
    /// <param name="ConfirmedAt">Timestamp when the user confirmed the reset.</param>
    /// <param name="CreatedAt">Timestamp when the reset was created.</param>
    /// <param name="OriginalLevel">The user's level before the reset, from 1 to 60</param>
    /// <param name="TargetLevel">The user's level after the reset, from 1 to 60. It must be less than or equal to original_level.</param>
    public record ResetData(
        DateTime? ConfirmedAt,
        DateTime CreatedAt,
        int OriginalLevel,
        int TargetLevel);
    
    #endregion

    #region review-data-structure

    /// <summary></summary>
    /// <param name="AssignmentId">Unique identifier of the associated assignment.</param>
    /// <param name="CreatedAt">Timestamp when the review was created.</param>
    /// <param name="EndingSrsStage">The SRS stage interval calculated from the number of correct and incorrect answers, with valid values ranging from 1 to 9</param>
    /// <param name="IncorrectMeaningAnswers">The number of times the user has answered the meaning incorrectly.</param>
    /// <param name="IncorrectReadingAnswers">The number of times the user has answered the reading incorrectly.</param>
    /// <param name="SpacedRepetitionSystemId">Unique identifier of the associated spaced_repetition_system.</param>
    /// <param name="StartingSrsStage">The starting SRS stage interval, with valid values ranging from 1 to 8</param>
    /// <param name="SubjectId">Unique identifier of the associated subject.</param>
    public record ReviewData(
        int AssignmentId,
        DateTime CreatedAt,
        int EndingSrsStage,
        int IncorrectMeaningAnswers,
        int IncorrectReadingAnswers,
        int SpacedRepetitionSystemId,
        int StartingSrsStage,
        int SubjectId);
    
    #endregion

    #region review-statistic-data-structure

    /// <summary></summary>
    /// <param name="CreatedAt">Timestamp when the review statistic was created.</param>
    /// <param name="Hidden">Indicates if the associated subject has been hidden, preventing it from appearing in lessons or reviews.</param>
    /// <param name="MeaningCorrect">Total number of correct answers submitted for the meaning of the associated subject.</param>
    /// <param name="MeaningCurrentStreak">The current, uninterrupted series of correct answers given for the meaning of the associated subject.</param>
    /// <param name="MeaningIncorrect">Total number of incorrect answers submitted for the meaning of the associated subject.</param>
    /// <param name="MeaningMaxStreak">The longest, uninterrupted series of correct answers ever given for the meaning of the associated subject.</param>
    /// <param name="PercentageCorrect">The overall correct answer rate by the user for the subject, including both meaning and reading.</param>
    /// <param name="ReadingCorrect">Total number of correct answers submitted for the reading of the associated subject.</param>
    /// <param name="ReadingCurrentStreak">The current, uninterrupted series of correct answers given for the reading of the associated subject.</param>
    /// <param name="ReadingIncorrect">Total number of incorrect answers submitted for the reading of the associated subject.</param>
    /// <param name="ReadingMaxStreak">The longest, uninterrupted series of correct answers ever given for the reading of the associated subject.</param>
    /// <param name="SubjectId">Unique identifier of the associated subject.</param>
    /// <param name="SubjectType">The type of the associated subject, one of: kanji, radical, or vocabulary.</param>
    public record ReviewStatisticData(
        DateTime CreatedAt,
        bool Hidden,
        int MeaningCorrect,
        int MeaningCurrentStreak,
        int MeaningIncorrect,
        int MeaningMaxStreak,
        int PercentageCorrect,
        int ReadingCorrect,
        int ReadingCurrentStreak,
        int ReadingIncorrect,
        int ReadingMaxStreak,
        int SubjectId,
        string SubjectType);
    
    #endregion

    #region spaced-repetition-system-data-structure

    /// <summary></summary>
    /// <param name="BurningStagePosition">position of the burning stage.</param>
    /// <param name="CreatedAt">Timestamp when the spaced_repetition_system was created.</param>
    /// <param name="Description">Details about the spaced repetition system.</param>
    /// <param name="Name">The name of the spaced repetition system</param>
    /// <param name="PassingStagePosition">position of the passing stage.</param>
    /// <param name="Stages">A collection of stages. See table below for the object structure.</param>
    /// <param name="StartingStagePosition">position of the starting stage.</param>
    /// <param name="UnlockingStagePosition">position of the unlocking stage.</param>
    public record SpacedRepetitionSystemData(
        int BurningStagePosition,
        DateTime CreatedAt,
        string Description,
        string Name,
        int PassingStagePosition,
        StagesObjectAttributes[] Stages,
        int StartingStagePosition,
        int UnlockingStagePosition);
    
    /// <summary></summary>
    /// <param name="Interval">The length of time added to the time of review registration, adjusted to the beginning of the hour.</param>
    /// <param name="IntervalUnit">Unit of time. Can be the following: milliseconds, seconds, minutes, hours, days, and weeks.</param>
    /// <param name="Position">The position of the stage within the continuous order.</param>
    public record StagesObjectAttributes(
        int? Interval,
        string? IntervalUnit,
        int Position);
    
    #endregion

    #region study-material-data-structure

    /// <summary></summary>
    /// <param name="CreatedAt">Timestamp when the study material was created.</param>
    /// <param name="Hidden">Indicates if the associated subject has been hidden, preventing it from appearing in lessons or reviews.</param>
    /// <param name="MeaningNote">Free form note related to the meaning(s) of the associated subject.</param>
    /// <param name="MeaningSynonyms">Synonyms for the meaning of the subject. These are used as additional correct answers during reviews.</param>
    /// <param name="ReadingNote">Free form note related to the reading(s) of the associated subject.</param>
    /// <param name="SubjectId">Unique identifier of the associated subject.</param>
    /// <param name="SubjectType">The type of the associated subject, one of: kanji, radical, or vocabulary.</param>
    public record StudyMaterialData(
        DateTime CreatedAt,
        bool Hidden,
        string? MeaningNote,
        object[] MeaningSynonyms,
        string? ReadingNote,
        int SubjectId,
        string SubjectType);
    
    #endregion

    #region subject-data-structure

    /// <summary></summary>
    /// <param name="AuxiliaryMeanings">Collection of auxiliary meanings. See table below for the object structure.</param>
    /// <param name="Characters">The UTF-8 characters for the subject, including kanji and hiragana.</param>
    /// <param name="CreatedAt">Timestamp when the subject was created.</param>
    /// <param name="DocumentUrl">A URL pointing to the page on wanikani.com that provides detailed information about this subject.</param>
    /// <param name="HiddenAt">Timestamp when the subject was hidden, indicating associated assignments will no longer appear in lessons or reviews and that the subject page is no longer visible on wanikani.com.</param>
    /// <param name="LessonPosition">The position that the subject appears in lessons. Note that the value is scoped to the level of the subject, so there are duplicate values across levels.</param>
    /// <param name="Level">The level of the subject, from 1 to 60.</param>
    /// <param name="MeaningMnemonic">The subject's meaning mnemonic.</param>
    /// <param name="Meanings">The subject meanings. See table below for the object structure.</param>
    /// <param name="Slug">The string that is used when generating the document URL for the subject. Radicals use their meaning, downcased. Kanji and vocabulary use their characters.</param>
    /// <param name="SpacedRepetitionSystemId">Unique identifier of the associated spaced_repetition_system.</param>
    public record SubjectData(
        AuxiliaryMeaningObjectAttributes[] AuxiliaryMeanings,
        string Characters,
        DateTime CreatedAt,
        string DocumentUrl,
        DateTime? HiddenAt,
        int LessonPosition,
        int Level,
        string MeaningMnemonic,
        MeaningObjectAttributes[] Meanings,
        string Slug,
        int SpacedRepetitionSystemId);
    
    /// <summary></summary>
    /// <param name="Meaning">A singular subject meaning.</param>
    /// <param name="Primary">Indicates priority in the WaniKani system.</param>
    /// <param name="AcceptedAnswer">Indicates if the meaning is used to evaluate user input for correctness.</param>
    public record MeaningObjectAttributes(
        string Meaning,
        bool Primary,
        bool AcceptedAnswer);
    
    /// <summary></summary>
    /// <param name="Meaning">A singular subject meaning.</param>
    /// <param name="Type">Either whitelist or blacklist. When evaluating user input, whitelisted meanings are used to match for correctness. Blacklisted meanings are used to match for incorrectness.</param>
    public record AuxiliaryMeaningObjectAttributes(
        string Meaning,
        string Type);
    
    /// <summary></summary>
    /// <param name="AmalgamationSubjectIds">An array of numeric identifiers for the kanji that have the radical as a component.</param>
    /// <param name="Characters">Unlike kanji and vocabulary, radicals can have a null value for characters. Not all radicals have a UTF entry, so the radical must be visually represented with an image instead.</param>
    /// <param name="CharacterImages">A collection of images of the radical. See table below for the object structure.</param>
    public record RadicalAttributes(
        int[] AmalgamationSubjectIds,
        string? Characters,
        CharacterImageObjectAttributes[] CharacterImages);
    
    /// <summary></summary>
    /// <param name="Url">The location of the image.</param>
    /// <param name="ContentType">The content type of the image. Currently the API delivers image/png and image/svg+xml.</param>
    /// <param name="Metadata">Details about the image. Each content_type returns a uniquely structured object.</param>
    public record CharacterImageObjectAttributes(
        string Url,
        string ContentType,
        PronunciationAudioMetadataObjectAttributes Metadata);
    
    /// <summary></summary>
    /// <param name="InlineStyles">The SVG asset contains built-in CSS styling</param>
    public record WhenContentTypeIsCodeImageSvgXmlCode(
        bool InlineStyles);
    
    /// <summary></summary>
    /// <param name="Color">Color of the asset in hexadecimal</param>
    /// <param name="Dimensions">Dimension of the asset in pixels</param>
    /// <param name="StyleName">A name descriptor</param>
    public record WhenContentTypeIsCodeImagePngCode(
        string Color,
        string Dimensions,
        string StyleName);
    
    /// <summary></summary>
    /// <param name="AmalgamationSubjectIds">An array of numeric identifiers for the vocabulary that have the kanji as a component.</param>
    /// <param name="ComponentSubjectIds">An array of numeric identifiers for the radicals that make up this kanji. Note that these are the subjects that must have passed assignments in order to unlock this subject's assignment.</param>
    /// <param name="MeaningHint">Meaning hint for the kanji.</param>
    /// <param name="ReadingHint">Reading hint for the kanji.</param>
    /// <param name="ReadingMnemonic">The kanji's reading mnemonic.</param>
    /// <param name="Readings">Selected readings for the kanji. See table below for the object structure.</param>
    /// <param name="VisuallySimilarSubjectIds">An array of numeric identifiers for kanji which are visually similar to the kanji in question.</param>
    public record KanjiAttributes(
        int[] AmalgamationSubjectIds,
        int[] ComponentSubjectIds,
        string? MeaningHint,
        string? ReadingHint,
        string ReadingMnemonic,
        ReadingObjectAttributes[] Readings,
        int[] VisuallySimilarSubjectIds);
    
    /// <summary></summary>
    /// <param name="Reading">A singular subject reading.</param>
    /// <param name="Primary">Indicates priority in the WaniKani system.</param>
    /// <param name="AcceptedAnswer">Indicates if the reading is used to evaluate user input for correctness.</param>
    /// <param name="Type">The kanji reading's classfication: kunyomi, nanori, or onyomi.</param>
    public record ReadingObjectAttributes(
        string Reading,
        bool Primary,
        bool AcceptedAnswer,
        string Type);
    
    /// <summary></summary>
    /// <param name="ComponentSubjectIds">An array of numeric identifiers for the kanji that make up this vocabulary. Note that these are the subjects that must be have passed assignments in order to unlock this subject's assignment.</param>
    /// <param name="ContextSentences">A collection of context sentences. See table below for the object structure.</param>
    /// <param name="MeaningMnemonic">The subject's meaning mnemonic.</param>
    /// <param name="PartsOfSpeech">Parts of speech.</param>
    /// <param name="PronunciationAudios">A collection of pronunciation audio. See table below for the object structure.</param>
    /// <param name="Readings">Selected readings for the vocabulary. See table below for the object structure.</param>
    /// <param name="ReadingMnemonic">The subject's reading mnemonic.</param>
    public record VocabularyAttributes(
        int[] ComponentSubjectIds,
        ContextSentenceObjectAttributes[] ContextSentences,
        string MeaningMnemonic,
        string[] PartsOfSpeech,
        PronunciationAudioObjectAttributes[] PronunciationAudios,
        ReadingObjectAttributes[] Readings,
        string ReadingMnemonic);
    
    /// <summary></summary>
    /// <param name="En">English translation of the sentence</param>
    /// <param name="Ja">Japanese context sentence</param>
    public record ContextSentenceObjectAttributes(
        string En,
        string Ja);
    
    /// <summary></summary>
    /// <param name="Url">The location of the audio.</param>
    /// <param name="ContentType">The content type of the audio. Currently the API delivers audio/mpeg and audio/ogg.</param>
    /// <param name="Metadata">Details about the pronunciation audio. See table below for details.</param>
    public record PronunciationAudioObjectAttributes(
        string Url,
        string ContentType,
        PronunciationAudioMetadataObjectAttributes Metadata);
    
    /// <summary></summary>
    /// <param name="Gender">The gender of the voice actor.</param>
    /// <param name="SourceId">A unique ID shared between same source pronunciation audio.</param>
    /// <param name="Pronunciation">Vocabulary being pronounced in kana.</param>
    /// <param name="VoiceActorId">A unique ID belonging to the voice actor.</param>
    /// <param name="VoiceActorName">Humanized name of the voice actor.</param>
    /// <param name="VoiceDescription">Description of the voice.</param>
    public record PronunciationAudioMetadataObjectAttributes(
        string Gender,
        int SourceId,
        string Pronunciation,
        int VoiceActorId,
        string VoiceActorName,
        string VoiceDescription);
    
    #endregion

    #region summary-data-structure

    /// <summary></summary>
    /// <param name="Lessons">Details about subjects available for lessons. See table below for object structure.</param>
    /// <param name="NextReviewsAt">Earliest date when the reviews are available. Is null when the user has no reviews scheduled.</param>
    /// <param name="Reviews">Details about subjects available for reviews now and in the next 24 hours by the hour (total of 25 objects). See table below for object structure.</param>
    public record SummaryData(
        LessonObjectAttributes[] Lessons,
        DateTime? NextReviewsAt,
        ReviewObjectAttributes[] Reviews);
    
    /// <summary></summary>
    /// <param name="AvailableAt">When the paired subject_ids are available for lessons. Always beginning of the current hour when the API endpoint is accessed.</param>
    /// <param name="SubjectIds">Collection of unique identifiers for subjects.</param>
    public record LessonObjectAttributes(
        DateTime AvailableAt,
        int[] SubjectIds);
    
    /// <summary></summary>
    /// <param name="AvailableAt">When the paired subject_ids are available for reviews. All timestamps are the top of an hour.</param>
    /// <param name="SubjectIds">Collection of unique identifiers for subjects.</param>
    public record ReviewObjectAttributes(
        DateTime AvailableAt,
        int[] SubjectIds);
    
    #endregion

    #region user-data-structure

    /// <summary></summary>
    /// <param name="CurrentVacationStartedAt">If the user is on vacation, this will be the timestamp of when that vacation started. If the user is not on vacation, this is null.</param>
    /// <param name="Level">The current level of the user. This ignores subscription status.</param>
    /// <param name="Preferences">User settings specific to the WaniKani application. See table below for the object structure.</param>
    /// <param name="ProfileUrl">The URL to the user's public facing profile page.</param>
    /// <param name="StartedAt">The signup date for the user.</param>
    /// <param name="Subscription">Details about the user's subscription state. See table below for the object structure.</param>
    /// <param name="Username">The user's username.</param>
    public record UserData(
        DateTime? CurrentVacationStartedAt,
        int Level,
        PreferencesObjectAttributes Preferences,
        string ProfileUrl,
        DateTime StartedAt,
        SubscriptionObjectAttributes Subscription,
        string Username);
    
    /// <summary></summary>
    /// <param name="DefaultVoiceActorId">The voice actor to be used for lessons and reviews. The value is associated to subject.pronunciation_audios.metadata.voice_actor_id.</param>
    /// <param name="LessonsAutoplayAudio">Automatically play pronunciation audio for vocabulary during lessons.</param>
    /// <param name="LessonsBatchSize">Number of subjects introduced to the user during lessons before quizzing.</param>
    /// <param name="LessonsPresentationOrder">The order in which lessons are presented. The options are ascending_level_then_subject, shuffled, and ascending_level_then_shuffled. The default (and best experience) is ascending_level_then_subject.</param>
    /// <param name="ReviewsAutoplayAudio">Automatically play pronunciation audio for vocabulary during reviews.</param>
    /// <param name="ReviewsDisplaySrsIndicator">Toggle for display SRS change indicator after a subject has been completely answered during review.</param>
    public record PreferencesObjectAttributes(
        int DefaultVoiceActorId,
        bool LessonsAutoplayAudio,
        int LessonsBatchSize,
        string LessonsPresentationOrder,
        bool ReviewsAutoplayAudio,
        bool ReviewsDisplaySrsIndicator);
    
    /// <summary></summary>
    /// <param name="Active">Whether or not the user currently has a paid subscription.</param>
    /// <param name="MaxLevelGranted">The maximum level of content accessible to the user for lessons, reviews, and content review. For unsubscribed/free users, the maximum level is 3. For subscribed users, this is 60. Any application that uses data from the WaniKani API must respect these access limits.</param>
    /// <param name="PeriodEndsAt">The date when the user's subscription period ends. If the user has subscription type lifetime or free then the value is null.</param>
    /// <param name="Type">The type of subscription the user has. Options are following: free, recurring, and lifetime.</param>
    public record SubscriptionObjectAttributes(
        bool Active,
        int MaxLevelGranted,
        DateTime? PeriodEndsAt,
        SubscriptionType Type);
    
    #endregion

    #region voice-actor-data-structure

    /// <summary></summary>
    /// <param name="Description">Details about the voice actor.</param>
    /// <param name="Gender">male or female</param>
    /// <param name="Name">The voice actor's name</param>
    public record VoiceActorData(
        string Description,
        string Gender,
        string Name);
    
    #endregion

}
