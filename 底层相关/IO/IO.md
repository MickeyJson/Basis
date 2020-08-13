# 何为IO:
* I/O就是输入和输出，核心是I/O流，流用于读写设备上的数据，包括硬盘文件、网络
	
# 用户空间与内核空间之分别：
* 都属于内存空间的一部分，但是操作系统为了支持多个应用同时运行，且保证不同进程之间的相对独立性，划分除了内核空间和用户空间
* 内核空间拥有更高的权限，为了保证安全性，且当一个进程崩溃时不会影响其他进程
	
# IO发生时设计的对象和步骤
* 调度IO的user progrecss(user Thread)
* System Kernel(系统内核)
* 步骤：
	* Read操作：
		1. 等待数据准备（waiting for the data to be ready）
		2. 将数据从内核拷贝到进程中（copying the data from the kernal to the progrecss）
			
# IO有哪几种：
* 阻塞IO(Blocking IO)
* 非阻塞IO(NonBlocking IO)
* 异步IO(Asynchronous IO)
* IO多路复用(IO Multiplexing)
	
	
* 阻塞IO(Blocking IO)
	* 阶段：
		1. <font color=yellow>用户发起read操作</font>
		2. <font color=yellow>内核准备数据</font>
		3. <font color=yellow>用户进程/线程就会将数据从内核拷贝到用户内存，然后内核返回结果</font>
	* 举例：
		* 当用户要读取网络数据时，就开始了第一个阶段：<font color=yellow>内核准备数据</font>,对于网络IO来说，很多时候数据在用户调用方法时还没有完全到达（比如，还没有收到一个完整的UDP包），这个时候内核就要等待数据完整到达。用户进程此时会被<font color=yellow>"阻塞"</font>。
		* 当内核中数据完整到达了，进入第二阶段，用户进程/线程就会将数据从内核拷贝到用户内存，然后内核返回结果，用户进程才<font color=yellow>"解除block的状态"</font>，继续运行。
	Blocking IO的特点就是在IO执行的两个阶段都被block了。
	
* 非阻塞 Non-Blocking IO
	* 阶段：
		1. <font color=yellow>用户发起read操作</font>
		2. <font color=yellow>内核检查数据是否准备好，没有准备好返回error，准备好直接返回结果</font>
		3. <font color=yellow>轮询check内核返回结果是否为error</font>
	
	* 当用户进程发出read操作时，<font color=red>如果kernel中的数据还没有准备好，那么它并不会block用户进程，而是立刻返回一个error</font>。从用户进程角度讲 ，它发起一个read操作后，并不需要等待，而是马上就得到了一个结果。用户进程判断结果是一个error时，它就知道数据还没有准备好，于是它可以再次发送read操作。一旦kernel中的数据准备好了，并且又再次收到了用户进程的read，那么内核马上就将数据拷贝到用户内存，然后返回结果。
	* 用户进程其实是需要不断的主动询问kernel数据好了没有，也叫轮询IO，特点是第一阶段不阻塞，第二阶段阻塞，并不是完完全全的非阻塞。

* 异步 Asynchronous IO
	* 阶段：
		1. <font color=yellow>发起read操作，不等待结果，接着做后面的事情</font>
		2. <font color=yellow>kernal收到异步读请求，立刻返回，等待数据准备完成，拷贝数据到user memory space,向user progrecss发送标记</font>
		3. <font color=yellow>用户进程收到标记之后读取user space中的数据，进行操作</font>

* 多路复用 IO Multiplexing
	* select/epoll的好处就在于单个process就可以同时处理多个网络连接的IO
		* 基本原理：
			* select/epoll方法会阻塞用户线程，然后内核会不断的轮询所负责的所有socket，一旦发现进程指定的一个或者多个Socket可以读写数据了，它就通知该进程，然后进程读写数据继续运行
	* 区别于阻塞/非阻塞IO
		* 也就是说用户的读写请求会被select方法全部hold住，整个过程还是阻塞的，只是原来是被内核hold住，现在是被select挡住。
		* 好处是原来一个进程/线程只能处理一个连接，那么这个连接阻塞的时间就被浪费了，多路复用把这些时间用来服务别的Connection了

* 同步IO代表会的阻塞IO（阻塞、非阻塞、多路复用）
	* 非阻塞IO并非完全的<font color=yellow>“非”</font>阻塞，只是第一阶段不阻塞，当数据准备好，还是会blocking，所以也属于同步IO
	* 多路复用与上类似
* 异步IO，一定是非阻塞
	* 2个阶段都由内核操作，内核check，内核拷贝数据，内核最后通知你