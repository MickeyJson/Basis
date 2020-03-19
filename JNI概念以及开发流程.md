* 何为JNI？
	* 全称 Java Native Interface ， java本地化接口 ， 可以通过JNI调用系统提供的API
* 工作机制？
	* JNI通过JVM虚拟机调用各平台的api
* 开发流程：
1. 编写本地java方法
	```
	public static native String getStringFromC() ;
	```
2. 生成.h头文件，使用javah命令，这里需要对java命令了解，目前好像在最新java版本javah命令已被删除，因为项目用的版本较低，没有去验证改命令在高版本是否存在！
	```
	# 配置好java环境
	# 在控制台中进入工程src目录
	> cd  *****
	> javah com.cyou.pf 
	# com.cyou.pf，是全类名，包名.类名
	```
3. 将.h头文件复制到VS的代码文件目录下 ， 在解决方案中的头文件目录-> 右键-> 添加 -> 添加现有项,并且引入头文件
4. 实现头文件
	```
	// 生成的头文件函数
	/*
	* Class:     com_zeno_jni_HelloJni
	* Method:    getStringFormC
	* Signature: ()Ljava/lang/String;
	*/
	JNIEXPORT jstring JNICALL Java_com.cyou.pf_HelloJni_getStringFromC
	(JNIEnv *, jclass);
	```
5.引入头文件
	```
	/*
	* Class:     com.cyou.pf
	* Method:    getStringFormC
	* Signature: ()Ljava/lang/String;
	*/
	JNIEXPORT jstring JNICALL Java_com.cyou.pf_HelloJni_getStringFromC
	(JNIEnv *Env, jclass jclazz) {

		return (*Env)->NewStringUTF(Env, "Jni C String");
	}
	```
6. 生成动态库或者静态库
7. 运行时加载动态库或者静态库