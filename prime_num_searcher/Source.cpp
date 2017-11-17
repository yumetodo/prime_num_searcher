#include <iostream>
#include <vector>
#include <string>
#include <valarray>
#include <chrono>
#include <functional>
#include <algorithm>
#include <future>
#include <thread>
#include <limits>
#include <memory>
#include <cstdint>
#include <cmath>
#include "version.h"
using prime_store_t = std::size_t;
using prime_vector = std::vector<prime_store_t>;
class prime_num_searcher
{
public:
	prime_num_searcher() = delete;
	prime_num_searcher(prime_store_t generate_max, const std::function<prime_vector(prime_store_t)>& searcher, const std::string& searcher_name)
		: generate_max_(generate_max), searcher_(searcher), searcher_name_(searcher_name), process_time(), prime_num_()
	{}
	prime_num_searcher(const prime_num_searcher&) = delete;
	prime_num_searcher(prime_num_searcher&&) = delete;
	prime_num_searcher& operator=(const prime_num_searcher&) = delete;
	prime_num_searcher& operator=(prime_num_searcher&&) = delete;
	void operator ()() {
		namespace ch = std::chrono;
		const auto start = ch::high_resolution_clock::now();
		this->prime_num_ = this->searcher_(this->generate_max_);
		const auto stop = ch::high_resolution_clock::now();
		this->process_time = ch::duration_cast<ch::nanoseconds>(stop - start);
	}
	void print(std::ostream& os) const {
		if (prime_num_.empty()) {
			os << "not calculated" << std::endl;
		}
		else {
			namespace ch = std::chrono;
			os
				<< "searcher_name:" << this->searcher_name_
				<< ",N:" << this->generate_max_
				<< ",numof:" << this->prime_num_.size()
				<< ",time(ms):" << ch::duration_cast<ch::milliseconds>(this->process_time).count()
				<< ",time(ns):" << this->process_time.count()
				<< std::endl;
		}
	}
private:
	prime_store_t generate_max_;
	std::function<prime_vector(prime_store_t)> searcher_;
	std::string searcher_name_;
	std::chrono::nanoseconds process_time;
	prime_vector prime_num_;
};
std::ostream& operator<<(std::ostream& os, const prime_num_searcher& ps) {
	ps.print(os);
	return os;
}

prime_vector sieve_of_eratosthenes(const prime_store_t generate_max) {//エラトステネスのふるい
	if (generate_max <= 1u) return {};
	prime_vector prime_num;
	prime_num.push_back(2);
	for (prime_store_t i = 3; i <= generate_max; i += 2) {
		auto it = prime_num.begin();
		const auto limit = prime_store_t(std::sqrt(i));
		//調査対象数iを上回る既知の素数で割ろうとするか、既知の素数で割り切れるまでイテレータを進める
		for (; it != prime_num.end() && *it <= limit && i % *it; it++);
		if (i % *it) {//既知の素数で割った余りがすべて0でないならば
			prime_num.push_back(i);
		}
		if (i == std::numeric_limits<prime_store_t>::max()) break;//iのオーバーフロー対策
	}
	return prime_num;
}
prime_vector simple_algrism(const prime_store_t generate_max) {
	prime_vector prime_num;
	auto p = std::make_unique<char[]>(generate_max + 1);
	p[0] = p[1] = 1;
	for (size_t j = 2; j < generate_max; j++) {
		for (size_t k = j + j; k <= generate_max; k += j) p[k] = 1;
	}
	for (size_t j = 0; j <= generate_max; j++) if (!p[j]) prime_num.push_back(j);
	return prime_num;
}
prime_vector simple_algrism_mt(const prime_store_t generate_max) {
	std::vector<std::unique_ptr<std::uint8_t[]>> p;
	std::vector<std::future<std::unique_ptr<std::uint8_t[]>>> threads;
	const auto thread_num = std::thread::hardware_concurrency();
	if (thread_num < 2) return simple_algrism(generate_max);
	p.reserve(thread_num);
	threads.reserve(thread_num);
	for (std::uint8_t tid = 0; tid < thread_num; ++tid) {
		threads.emplace_back(std::async(
			std::launch::async,
			[generate_max, thread_num](std::uint8_t tid) {
				auto p = std::make_unique<std::uint8_t[]>(generate_max + 1);
				p[0] = p[1] = 1;
				for (size_t j = 2 + tid; j < generate_max; j += thread_num) {
					for (size_t k = j + j; k <= generate_max; k += j) p[k] = 1;
				}
				return p;
			},
			tid
		));
	}
	for (auto&& t : threads) {
		p.emplace_back(t.get());
	}
	auto is_prime = [&p](std::size_t index) -> bool {
		for (auto&& pn : p) if (pn[index]) return false;
		return true;
	};
	prime_vector prime_num;
	for (size_t j = 0; j <= generate_max; j++) if (is_prime(j)) prime_num.push_back(j);
	return prime_num;
}
prime_vector forno_method(const prime_store_t limit) {
	prime_vector prime_num;
	std::valarray<bool> prims(true, limit + 1); // array of [0] to [limit]
	prims[std::slice(0, 2, 1)] = false; // 0 and 1 aren't primes. (not needed)

	const prime_store_t search_limit{ static_cast<prime_store_t>(std::floor(std::sqrt(static_cast<long double>(limit)))) + 1 };
	for (prime_store_t i{ 2 }; i < search_limit; ++i) if (prims[i]) {
		prims[std::slice(i*i, limit / i - (i - 1), i)] = false;
	}
	for (prime_store_t i{ 2 }; i <= limit; ++i) if (prims[i]) prime_num.push_back(i);
	return prime_num;
}
int main(int argc, char* argv[]) {
	std::ios::sync_with_stdio(false);
	const auto generate_max = [argc, argv] {
		prime_store_t tmp = 0;
		auto read_from_stdin = [] {
			std::cout << "求める素数の最大値を入力してください" << std::endl;
			prime_store_t tmp = 0;
			std::cin >> tmp;
			return (tmp > 2) ? tmp : 3;
		};
		if (2 == argc) {
			using namespace std::string_literals;
			if ("-v"s == argv[1] || "--version"s == argv[1]) {
				std::cout << "prime num searcher version " PRIME_NUM_VERSION_STR << std::endl;
			}
			try {
				const auto t = std::stoull(argv[1]);
				tmp = (std::numeric_limits<prime_store_t>::max() < t) ? read_from_stdin() : prime_store_t(t);
			}
			catch (...) {
				tmp = read_from_stdin();
			}
		}
		else {
			tmp = read_from_stdin();
		}
		return tmp;
	}();
	prime_num_searcher prime_num_searchers[] = {
		{ generate_max, sieve_of_eratosthenes, "sieve_of_eratosthenes" },
		{ generate_max, simple_algrism, "simple_algrism" },
		{ generate_max, simple_algrism_mt, "simple_algrism_mt" },
		{ generate_max, forno_method, "forno_method" }
	};
	for (auto&& prime_num_searcher : prime_num_searchers) prime_num_searcher();
	for (auto&& re : prime_num_searchers) std::cout << re;
	return 0;
}
