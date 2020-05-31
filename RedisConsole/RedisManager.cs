using ServiceStack.Redis;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace RedisConsole
{
    /// <summary>
    /// RedisManager类主要是创建链接池管理对象的
    /// </summary>
    public class RedisManager
    {
        /// <summary>
        /// 绑定本机Redis
        /// </summary>
        private static string ConnStr = "123456@localhost:6379";//password@ip:port  123@localhost:6379
        private static PooledRedisClientManager _prcm;

        
        /// <summary>
        /// 静态构造方法，初始化链接池管理对象
        /// </summary>
        static RedisManager()
        {
            _prcm = CreateManager(new string[] { ConnStr }, new string[] { ConnStr });
        }
        /// <summary>
        /// 创建链接池管理对象
        /// </summary> 
        private static PooledRedisClientManager CreateManager(string[] readWriteHosts, string[] readOnlyHosts, int initalDb = 0)
        {
            //WriteServerList：可写的Redis链接地址。
            //ReadServerList：可读的Redis链接地址。
            //MaxWritePoolSize：最大写链接数。
            //MaxReadPoolSize：最大读链接数。
            //AutoStart：自动重启。
            //LocalCacheTime：本地缓存到期时间，单位:秒。
            //RecordeLog：是否记录日志,该设置仅用于排查redis运行时出现的问题,如redis工作正常,请关闭该项。
            //RedisConfigInfo类是记录redis连接信息，此信息和配置文件中的RedisConfig相呼应
            // 支持读写分离，均衡负载 
            return new PooledRedisClientManager(readWriteHosts, readOnlyHosts, new RedisClientManagerConfig
            {
                MaxWritePoolSize = 5, // “写”链接池链接数 
                MaxReadPoolSize = 5, // “读”链接池链接数 
                AutoStart = true,
            },
            initalDb,//初始化数据库 默认有16个数据 这里设置初始化为第0个
            50,//连接池数量
            5//连接池超时秒数
            )
            {
                ConnectTimeout = 6000,//连接超时时间，毫秒
                SocketSendTimeout = 6000,//数据发送超时时间，毫秒
                SocketReceiveTimeout = 6000,// 数据接收超时时间，毫秒
                IdleTimeOutSecs = 60,//连接最大的空闲时间 默认是240
                PoolTimeout = 6000 //连接池取链接的超时时间，毫秒
            };
        }

        /// <summary>
        /// 客户端缓存操作对象
        /// </summary>
        public static IRedisClient GetClient()
        {
            if (_prcm == null)
            {
                _prcm = CreateManager(new string[] { ConnStr }, new string[] { ConnStr });
            } 
            return _prcm.GetClient();
        }
        /// <summary>
        /// 取得服务器主机地址集合。
        /// </summary>
        /// <param name="serverHosts">服务器主机地址。</param>
        /// <param name="split">分隔符。</param>
        /// <returns>服务器主机地址集合。</returns>
        private static string[] SplitServerHosts(string serverHosts, string split)
        {
            return serverHosts.Split(split.ToArray());
        }

        /// <summary>
        /// 获取 Redis 客户端缓存操作对象。
        /// </summary>
//        public static IRedisClient GetClient()
//        {
//            if (_redisClientsManager == null)
//                CreateManager();
//            var redisClient = _redisClientsManager.GetClient();
//            redisClient.Password = _soukeRedisConfiguration.SentinelPassword;
//#if(DEBUG)
//            redisClient.Db = _soukeRedisConfiguration.SentinelDb;
//#endif
//            return redisClient;
//        }

        /// <summary>
        /// 刷新全部数据。
        /// </summary>
        public static void FlushAll()
        {
            using (IRedisClient redis = GetClient())
            {
                redis.FlushAll();
            }
        }

        #region 项...

        /// <summary>
        /// 设置指定的项到指定的键中。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t">指定的项。</param>
        /// <returns>如果设置成功，则为 true；否则为 false。</returns>
        public static bool ItemSet<T>(string key, T t)
        {
            using (IRedisClient redis = GetClient())
            {
                return redis.Set(key, t);
            }
        }

        /// <summary>
        /// 设置指定的项到指定的键中。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t">指定的项。</param>
        /// <param name="expire">指定的有效期（单位：分钟）。</param>
        /// <returns>如果设置成功，则为 true；否则为 false。</returns>
        public static bool ItemSetExpire<T>(string key, T t, int expire)
        {
            using (IRedisClient redis = GetClient())
            {
                return redis.Set(key, t, DateTime.Now.Add(TimeSpan.FromMinutes(expire)));
            }
        }

        /// <summary>
        /// 获取指定的键的项。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <returns>如果存在该项，则返回该项，否则返回 null。</returns>
        public static T ItemGet<T>(string key) where T : class
        {
            using (IRedisClient redis = GetClient())
            {
                return redis.Get<T>(key);
            }
        }

        /// <summary>
        /// 移除指定的键的项。
        /// </summary>
        /// <param name="key">指定的键。</param>
        /// <returns>如果移除成功，则为 true；否则为 false。</returns>
        public static bool ItemRemove(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                return redis.Remove(key);
            }
        }

        #endregion

        #region 列表...

        /// <summary>
        /// 添加项到指定键的列表。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t">指定的项。</param>
        public static void ListAdd<T>(string key, T t)
        {
            using (IRedisClient redis = GetClient())
            {
                var redisTypedClient = redis.As<T>();
                redisTypedClient.AddItemToList(redisTypedClient.Lists[key], t);
            }
        }

        /// <summary>
        /// 将指定的对象集合的元素添加到指定键的列表末尾。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="tList">指定的项的集合。</param>
        public static void ListAddRange<T>(string key, List<T> tList)
        {
            using (IRedisClient redis = GetClient())
            {
                var redisTypedClient = redis.As<T>();
                redisTypedClient.Lists[key].AddRange(tList);
            }
        }

        /// <summary>
        /// 从指定键的列表中移除指定的项。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t">指定的项。</param>
        /// <returns>返回一个值，该值表示是否移除成功。</returns>
        public static bool ListRemove<T>(string key, T t)
        {
            using (IRedisClient redis = GetClient())
            {
                var redisTypedClient = redis.As<T>();
                return redisTypedClient.RemoveItemFromList(redisTypedClient.Lists[key], t) > 0;
            }
        }

        /// <summary>
        /// 从指定键的列表中移除所有的项。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        public static void ListRemoveAll<T>(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                var redisTypedClient = redis.As<T>();
                redisTypedClient.Lists[key].RemoveAll();
            }
        }

        /// <summary>
        /// 获取指定键的列表中实际包含的元素数。
        /// </summary>
        /// <param name="key">指定的键。</param>
        /// <returns>指定的键中实际包含的元素数。</returns>
        public static int ListCount(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                return (int)redis.GetListCount(key);
            }
        }

        /// <summary>
        /// 从指定键的列表中获取指定范围的元素。
        /// </summary>
        /// <typeparam name="T">指定项的类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="start">范围开始处的从零开始的索引。</param>
        /// <param name="count">范围中的元素数。</param>
        /// <returns>指定键的列表中的范围的元素。</returns>
        public static List<T> ListGetRange<T>(string key, int index, int count)
        {
            using (IRedisClient redis = GetClient())
            {
                var c = redis.As<T>();
                return c.Lists[key].GetRange(index, index + count - 1);
            }
        }

        /// <summary>
        /// 获取指定键的列表中的所有元素。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <returns>指定键的列表中的范围的元素。</returns>
        public static List<T> ListGetAll<T>(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                var c = redis.As<T>();
                return c.Lists[key].GetRange(0, c.Lists[key].Count);
            }
        }

        /// <summary>
        /// 分页获取指定键的列表中的元素。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="pageIndex">指定的分页索引。</param>
        /// <param name="pageSize">指定的分页大小。</param>
        /// <returns>指定键的列表中的范围的元素。</returns>
        public static List<T> ListGetAll<T>(string key, int pageIndex, int pageSize)
        {
            int start = pageSize * (pageIndex - 1);
            return ListGetRange<T>(key, start, pageSize);
        }

        /// <summary>
        /// 设置指定键的列表的有效期。
        /// </summary>
        /// <param name="key">指定的键。</param>
        /// <param name="datetime">有效期。</param>
        public static void ListSetExpire(string key, DateTime datetime)
        {
            using (IRedisClient redis = GetClient())
            {
                redis.ExpireEntryAt(key, datetime);
            }
        }

        #endregion

        #region 集合...

        /// <summary>
        /// 将一个或多个元素加入到集合指定的键中，已经存在于集合的元素将被忽略。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t">指定的项。</param>
        public static void SetAdd<T>(string key, T t)
        {
            using (IRedisClient redis = GetClient())
            {
                var redisTypedClient = redis.As<T>();
                redisTypedClient.Sets[key].Add(t);
            }
        }

        /// <summary>
        /// 获取指定的键的集合中实际包含的元素数。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <returns>指定的键的集合中实际包含的元素数。</returns>
        public static int SetCount<T>(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                var redisTypedClient = redis.As<T>();
                return redisTypedClient.Sets[key].Count();
            }
        }

        /// <summary>
        /// 确定某项是否在指定键的集合中。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t">指定的项。</param>
        /// <returns>如果存在该项，则为 true；否则为 false。</returns>
        public static bool SetContains<T>(string key, T t)
        {
            using (IRedisClient redis = GetClient())
            {
                var redisTypedClient = redis.As<T>();
                return redisTypedClient.Sets[key].Contains(t);
            }
        }

        /// <summary>
        /// 从指定键的集合中移除特定对象的第一个匹配项。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t">指定的项。</param>
        /// <returns>如果移除成功，则为 true；否则为 false。</returns>
        public static bool SetRemove<T>(string key, T t)
        {
            using (IRedisClient redis = GetClient())
            {
                var redisTypedClient = redis.As<T>();
                return redisTypedClient.Sets[key].Remove(t);
            }
        }

        /// <summary>
        /// 获取指定键的数据集的所有项。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <returns>指定键的数据集的所有项</returns>
        public static IList<T> SetGetAll<T>(string key)
        {
            List<T> result = new List<T>();
            using (IRedisClient redis = GetClient())
            {
                var redisTypedClient = redis.As<T>();
                var sets = redisTypedClient.Sets[key];

                if (sets != null && sets.Count > 0)
                {
                    foreach (var item in sets)
                    {
                        result.Add(item);
                    }
                }
            }
            return result;
        }

        #endregion

        #region 哈希表...

        /// <summary>
        /// 确定某项是否在指定键的哈希表中。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="dataKey">数据项的键。</param>
        /// <returns>如果存在该项，则为 true；否则为 false。</returns>
        public static bool HashSetContains<T>(string key, string dataKey)
        {
            using (IRedisClient redis = GetClient())
            {
                return redis.HashContainsEntry(key, dataKey);
            }
        }

        /// <summary>
        /// 设置指定的项到指定键的哈希表中。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="dataKey">数据项的键。</param>
        /// <returns>如果设置成功，则为 true；否则为 false。</returns>
        public static bool HashSetAdd<T>(string key, string dataKey, T t)
        {
            using (IRedisClient redis = GetClient())
            {
                string value = JsonSerializer.SerializeToString(t);
                return redis.SetEntryInHash(key, dataKey, value);
            }
        }

        /// <summary>
        /// 移除哈希表中指定键的项。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="dataKey">数据项的键。</param>
        /// <returns>如果移除成功，则为 true；否则为 false。</returns>
        public static bool HashSetRemove(string key, string dataKey)
        {
            using (IRedisClient redis = GetClient())
            {
                return redis.RemoveEntryFromHash(key, dataKey);
            }
        }

        /// <summary>
        /// 移除哈希表中的所有项。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="dataKey">数据项的键。</param>
        /// <returns>如果移除成功，则为 true；否则为 false。</returns>
        public static bool HashSetRemoveAll(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                return redis.Remove(key);
            }
        }

        /// <summary>
        /// 获取哈希表中的指定键的项。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="dataKey">数据项的键。</param>
        /// <returns>指定键的哈希表的项。</returns>
        public static T HashSetGet<T>(string key, string dataKey)
        {
            using (IRedisClient redis = GetClient())
            {
                string value = redis.GetValueFromHash(key, dataKey);
                return JsonSerializer.DeserializeFromString<T>(value);
            }
        }

        /// <summary>
        /// 获取指定键的哈希表的所有项。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <returns>指定键的哈希表的所有项。</returns>
        public static List<T> HashSetGetAll<T>(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                var list = redis.GetHashValues(key);
                if (list != null && list.Count > 0)
                {
                    List<T> result = new List<T>();
                    foreach (var item in list)
                    {
                        var value = JsonSerializer.DeserializeFromString<T>(item);
                        result.Add(value);
                    }
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// 设置指定键的哈希表的有效期。
        /// </summary>
        /// <param name="key">指定的键。</param>
        /// <param name="expire">指定的有效期时间（单位：分钟）。</param>
        public static void HashSetExpire(string key, int expire)
        {
            using (IRedisClient redis = GetClient())
            {
                redis.ExpireEntryAt(key, DateTime.Now.Add(TimeSpan.FromMinutes(expire)));
            }
        }

        #endregion

        #region 有序集合...

        /// <summary>
        /// 添加指定项到指定键的有序集合。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t">指定的项。</param>
        /// <returns>如果成功添加，则为 true；否则为 false。</returns>
        public static bool SortedSetAdd<T>(string key, T t)
        {
            using (IRedisClient redis = GetClient())
            {
                string value = JsonSerializer.SerializeToString<T>(t);
                return redis.AddItemToSortedSet(key, value);
            }
        }


        /// <summary>
        /// 添加指定项到指定键的有序集合，评分用于排序。如果该元素已经存在，则根据评分更新该元素的顺序。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t">指定的项。</param>
        /// <param name="score">用于排序的评分值。</param>
        /// <returns>如果成功添加，则为 true；否则为 false。</returns>
        public static bool SortedSetAdd<T>(string key, T t, double score)
        {
            using (IRedisClient redis = GetClient())
            {
                string value = JsonSerializer.SerializeToString<T>(t);
                return redis.AddItemToSortedSet(key, value, score);
            }
        }

        /// <summary>
        /// 从指定键的有序集合中移除指定的元素。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t">指定的项。</param>
        /// <returns>如果成功移除，则为 true；否则为 false。</returns>
        public static bool SortedSetRemove<T>(string key, T t)
        {
            using (IRedisClient redis = GetClient())
            {
                string value = ServiceStack.Text.JsonSerializer.SerializeToString<T>(t);
                return redis.RemoveItemFromSortedSet(key, value);
            }
        }

        /// <summary>
        /// 从指定键的有序集合尾部移除指定的索引后的匹配项。
        /// </summary>
        /// <param name="key">指定的键。</param>
        /// <param name="size">保留的条数。</param>
        /// <returns>移除的元素数量。</returns>
        public static int SortedSetTrim(string key, int size)
        {
            using (IRedisClient redis = GetClient())
            {
                return (int)redis.RemoveRangeFromSortedSet(key, size, 9999999);
            }
        }

        /// <summary>
        /// 获取指定的键的有序集合中实际包含的元素数。
        /// </summary>
        /// <param name="key">指定的键。</param>
        /// <returns>指定的键的有序集合中实际包含的元素数。</returns>
        public static int SortedSetCount(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                return (int)redis.GetSortedSetCount(key);
            }
        }

        /// <summary>
        /// 分页获取指定键的有序集合中的元素。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="pageIndex">指定的分页索引。</param>
        /// <param name="pageSize">指定的分页大小。</param>
        /// <returns>指定键的有序集合中的元素集合。</returns>
        public static List<T> SortedSetGetList<T>(string key, int pageIndex, int pageSize)
        {
            using (IRedisClient redis = GetClient())
            {
                var list = redis.GetRangeFromSortedSet(key, (pageIndex - 1) * pageSize, pageIndex * pageSize - 1);
                if (list != null && list.Count > 0)
                {
                    List<T> result = new List<T>();
                    foreach (var item in list)
                    {
                        var data = ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(item);
                        result.Add(data);
                    }
                    return result;
                }
            }
            return null;
        }


        /// <summary>
        /// 获取指定键的有序集合中的所有元素。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <returns>指定键的有序集合中的元素集合。</returns>
        public static List<T> SortedSetGetListALL<T>(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                var list = redis.GetRangeFromSortedSet(key, 0, 9999999);
                if (list != null && list.Count > 0)
                {
                    List<T> result = new List<T>();
                    foreach (var item in list)
                    {
                        var data = JsonSerializer.DeserializeFromString<T>(item);
                        result.Add(data);
                    }
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// 设置指定键的有序集合的有效期。
        /// </summary>
        /// <param name="key">指定的键。</param>
        /// <param name="expire">指定的有效期（单位：分钟）。</param>
        public static void SortedSetSetExpire(string key, int expire)
        {
            using (IRedisClient redis = GetClient())
            {
                redis.ExpireEntryAt(key, DateTime.Now.Add(TimeSpan.FromMinutes(expire)));
            }
        }

        /// <summary>
        /// 获取指定键的有序集合中的指定元素的评分。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t">指定的项。</param>
        /// <returns>指定键的有序集合中的指定元素的评分。</returns>
        public static double SortedSetGetItemScore<T>(string key, T t)
        {
            using (IRedisClient redis = GetClient())
            {
                var data = ServiceStack.Text.JsonSerializer.SerializeToString<T>(t);
                return redis.GetItemScoreInSortedSet(key, data);
            }
        }

        #endregion

        #region 二进制...

        /// <summary>
        /// 设置 BitMap 值，返回 0 或 1 是成功，返回 -1 是失败。
        /// </summary>
        /// <param name="key">指定的键。</param>
        /// <param name="offset">偏移量。</param>
        /// <param name="value">值只能是 0 或 1，其它的值会报错</param>
        /// <returns>返回 0 或 1 是成功，返回 -1 是失败。</returns>
        public static long BitSet(string key, int offset, int value)
        {
            using (IRedisClient redis = GetClient())
            {
                var rc = redis as RedisClient;
                if (rc != null)
                {
                    return rc.SetBit(key, offset, value);
                }
                return -1;
            }
        }

        /// <summary>
        /// 获取 BitMap 值，返回 0 或 1 是成功，返回 -1 是失败。
        /// </summary>
        /// <param name="key">指定的键。</param>
        /// <param name="offset">偏移量。</param>
        /// <returns>返回 0 或 1 是成功，返回 -1 是失败。</returns>
        public static long BitGet(string key, int offset)
        {
            using (IRedisClient redis = GetClient())
            {
                var rc = redis as RedisClient;
                if (rc != null)
                {
                    return rc.GetBit(key, offset);
                }
                return -1;
            }
        }

        public bool ByteSet<T>(string key, T t)
        {
            bool result = false;
            if (t != null)
            {
                MemoryStream ms = new MemoryStream();
                Serialize(t, ms);
                byte[] data = ms.ToArray();
                using (IRedisClient redis = GetClient())
                {
                    return redis.Set(key, data);
                }
            }
            return result;

        }

        /// <summary>
        /// 写入一个实体类T，将其转换成二制进存储，并且数据过期时间一到则立刻删除
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <param name="t"></param>
        /// <param name="Expire"></param>
        /// <returns></returns>
        public bool ByteSetExpire<T>(string key, T t, DateTime Expire)
        {
            bool flag = false;
            if (t != null)
            {
                MemoryStream ms = new MemoryStream();
                Serialize<T>(t, ms);
                byte[] data = ms.ToArray();
                using (IRedisClient redis = GetClient())
                {
                    TimeSpan ts = Expire - DateTime.Now;
                    flag = redis.Set(key, data, ts);
                }
            }
            return flag;
        }

        /// <summary>
        /// 获取 key 值为 key 的实体类 T。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="key">指定的键。</param>
        /// <returns></returns>
        public T ByteGet<T>(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                byte[] data = redis.Get<byte[]>(key);
                if (data == null || data.Length == 0)
                {
                    return default(T);
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(data, 0, data.Length);
                    ms.Position = 0;
                    return Deserialize<T>(ms);
                }
            }
        }

        #endregion

        #region 其他方法...

        /// <summary>
        /// 获取当前服务器中的所有 Key。
        /// </summary>
        /// <returns>当前服务器中的所有 Key 的集合。</returns>
        public static List<string> GetAllKeys()
        {
            using (IRedisClient redis = GetClient())
            {
                return redis.GetAllKeys();
            }
        }

        /// <summary>
        /// 移除指定集合中的所有 Key。
        /// </summary>
        /// <param name="keys">指定的 Key 集合。</param>
        public static void RemoveAll(IEnumerable<string> keys)
        {
            using (IRedisClient redis = GetClient())
            {
                redis.RemoveAll(keys);
            }
        }

        /// <summary>
        /// 确定指定的键是否在 Redis 数据库中。
        /// </summary>
        /// <param name="key">指定的键。</param>
        /// <returns>若存在指定的键，则返回 true，否则返回 false。</returns>
        public static bool ContainsKey(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                return redis.ContainsKey(key);
            }
        }

        /// <summary>
        /// 获取当前数据库服务器中的指定的 Key。
        /// </summary>
        /// <param name="key">指定的键。</param>
        /// <returns>若移除指定的键成功，则返回 true，否则返回 false。</returns>
        public static bool Remove(string key)
        {
            using (IRedisClient redis = GetClient())
            {
                return redis.Remove(key);
            }
        }

        /// <summary>
        /// 获取当前 Redis 信息。
        /// </summary>
        /// <returns>当前 Redis 信息。</returns>
        public Dictionary<string, string> GetInfo()
        {
            using (IRedisClient redis = GetClient())
            {
                return redis.Info;
            }
        }

        /// <summary>
        /// 将指定的对象序列化成二进制对象。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="obj">指定的对象。</param>
        /// <param name="stream">序列化的二进制对象。</param>
        private static void Serialize<T>(T obj, Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
        }

        /// <summary>
        /// 将指定的二进制对象反序列化成对象。
        /// </summary>
        /// <typeparam name="T">指定的项类型。</typeparam>
        /// <param name="obj">指定的对象。</param>
        /// <param name="stream">序列化的二进制对象。</param>
        /// <returns>反序列化的对象。</returns>
        public static T Deserialize<T>(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            var formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }

        #endregion



    }

}
 
 