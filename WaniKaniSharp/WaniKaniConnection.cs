using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekogumi.WaniKaniSharp
{

    public partial class WaniKaniConnection : HttpConnection
    {
        private const string APIURL = "https://api.wanikani.com/v2/";

        public WaniKaniConnection(string apiKey, IETagCache? cache)
            : base(60, cache)
        {
            BaseAddress = new Uri(APIURL);
            Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
        }

        //internal Task<Resource<TData>> QueryAsync<TData>(CancellationToken cancellationToken
        //    , string endpoint
        //    , params (string name, object value)[] parameters)
        //    where TData : IResourceData
        //    => GetJSONAsync<Resource<TData>>(cancellationToken, endpoint, parameters);

        //internal async Task<IReadOnlyList<Resource<TData>>> QueryCollectionAsync<TData>(CancellationToken cancellationToken
        //    , string endpoint
        //    , bool singleton
        //    , params (string name, object value)[] parameters)
        //    where TData : IResourceData
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    if (singleton)
        //    {
        //        var response = await GetJSONAsync<Resource<TData>>(cancellationToken, endpoint, parameters).ConfigureAwait(false);
        //        if (response is null) return null;
        //        return new[] { response };
        //    }
        //    else
        //    {
        //        var response = await GetJSONAsync<ResourceCollection<TData>>(cancellationToken, endpoint, parameters).ConfigureAwait(false);
        //        if (response is null) return null;
        //        else if (response.pages.next_url is null) return response.data;
        //        else
        //        {
        //            var items = new List<Resource<TData>>(response.data);
        //            var localParameters = new (string name, object value)[(parameters?.Length ?? 0) + 1];
        //            if (parameters != null) Array.Copy(parameters, localParameters, parameters.Length);

        //            do
        //            {
        //                Debug.WriteLine(response.pages.next_url);
        //                localParameters[localParameters.Length - 1] = ("page_after_id", response.Last().id);
        //                response = await GetJSONAsync<ResourceCollection<TData>>(cancellationToken, endpoint, localParameters).ConfigureAwait(false);
        //                items.AddRange(response.data);
        //            } while (response.pages.next_url != null);
        //            return items;
        //        }
        //    }
        //}


    }
}
