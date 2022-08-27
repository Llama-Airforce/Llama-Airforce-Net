using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.SeedWork.Extensions;

namespace Llama.Airforce.Jobs.Extensions
{
    public static class PaginationExt
    {
        public static EitherAsync<Error, Lst<T>> Pagination<T>(
            Func<int, int, EitherAsync<Error, Lst<T>>> f,
            int page = 0,
            int offset = 1000)
        {
            var xs = f(page, offset);

            return xs.Bind(x =>
            {
                var next = x.Count() >= offset
                    ? Pagination(f, page + 1, offset)
                    : List.empty<T>().ToEitherAsync();

                return next.Map(n => x.Concat(n).toList());
            });
        }

        public static EitherAsync<Error, Lst<T>> Paginate<T>(
            this Func<int, int, EitherAsync<Error, Lst<T>>> f,
            int page = 0,
            int offset = 1000) => Pagination(f, page, offset);
    }
}