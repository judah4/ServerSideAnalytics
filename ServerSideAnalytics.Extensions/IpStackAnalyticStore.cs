﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServerSideAnalytics.Extensions
{
    internal class IpStackAnalyticStore : IAnalyticStore
    {
        private readonly string _accessKey;
        private readonly IAnalyticStore _store;

        public IpStackAnalyticStore(IAnalyticStore store, string token)
        {
            _accessKey = token;
            _store = store;
        }

        public Task<long> CountAsync(DateTime from, DateTime to) => _store.CountAsync(from, to);

        public Task<long> CountUniqueIndentitiesAsync(DateTime day) => _store.CountUniqueIndentitiesAsync(day);

        public Task<long> CountUniqueIndentitiesAsync(DateTime from, DateTime to) => _store.CountUniqueIndentitiesAsync(from, to);

        public Task<IEnumerable<WebRequest>> InTimeRange(DateTime from, DateTime to) => _store.InTimeRange(from, to);

        public Task<IEnumerable<IPAddress>> IpAddressesAsync(DateTime day) => _store.IpAddressesAsync(day);

        public Task<IEnumerable<IPAddress>> IpAddressesAsync(DateTime from, DateTime to) => _store.IpAddressesAsync(from,to);

        public Task PurgeGeoIpAsync() => _store.PurgeGeoIpAsync();

        public Task PurgeRequestAsync() => _store.PurgeRequestAsync();

        public Task<IEnumerable<WebRequest>> RequestByIdentityAsync(string identity) => _store.RequestByIdentityAsync(identity);

        public async Task<CountryCode> ResolveCountryCodeAsync(IPAddress address)
        {
            try
            {
                var resolved = await _store.ResolveCountryCodeAsync(address);

                if(resolved == CountryCode.World)
                {
                    var ipstr = address.ToString();
                    var response = await (new HttpClient()).GetStringAsync($"http://api.ipstack.com/{ipstr}?access_key={_accessKey}&format=1");

                    var obj = JsonConvert.DeserializeObject(response) as JObject;
                    resolved = (CountryCode)Enum.Parse(typeof(CountryCode), obj["country_code"].ToString());

                    await _store.StoreGeoIpRangeAsync(address, address, resolved);

                    return resolved;
                }

                return resolved;
            }
            catch (Exception)
            {
            }
            return CountryCode.World;
        }

        public Task StoreGeoIpRangeAsync(IPAddress from, IPAddress to, CountryCode countryCode)
        {
            return _store.StoreGeoIpRangeAsync(from, to, countryCode);
        }

        public Task StoreWebRequestAsync(WebRequest request)
        {
            return _store.StoreWebRequestAsync(request);
        }
    }
}
