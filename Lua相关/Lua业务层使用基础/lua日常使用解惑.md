# lua日常逻辑应用，问题记录
* 最近在调试时发现，服务器返回的是无序的table，也就是说内部是指定kv的，所以出现了一个小bug，就是获取数组长度有问题
	* 首先得说一下无序与有序的区分
		* 有序
			```
			local xiang = {10,22,34,42,51}
			print("xiang length ==",table.getn(xiang)) 
			结果为：[LUA-print] xiang length ==    5
			```
		* 无序
			```
			local song = {s=10,h=22,x=34,m=42,n=51}
			print("song length ==",table.getn(song)) 
			结果为：[LUA-print] song length ==     0
			```
	* Table count,Table底层分为数组与hash部分
		* 在实际应用中，不可避免要 #table 或者 table.getn(table)来获取一个table的长度，用来计算或者显示
		* table.getn(tableName) 得到一个table的大小，等同于操作符#。
			* 该table的key必须是有序的,也就是说只对数组有效的，并且一定是从1开始的，顺序递增查找
		* 如果是无序的，我们可以使用pairs遍历自己计算得到
			```
			local count = 0
			for k,v in pairs(song) do
				count = count + 1
			end
			print("song length ==",count) --结果为： [LUA-print] song length ==    5
			```
		* 最后还有另外一种方法可以得到无序/有序的长度
			* <b><font color="red">table.nums()</font></b> 则计算 table中所有不为 nil 的值的个数。