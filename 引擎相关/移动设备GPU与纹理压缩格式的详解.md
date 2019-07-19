# <font size="10">移动设备GPU与纹理压缩格式的详解</font>

# 移动设备GPU
* Imagination Technologies的PowerVR SGX系列
    * 代表型号：PowerVR SGX 535、PowerVR SGX 540、PowerVR SGX 543MP、PowerVR SGX 554MP等
    * 代表作  ：Apple iPhone全系、iPad全系，三星I9000、P3100等

* Qualcomm(高通)的Adreno系列
    * 代表型号：Adreno 200、Adreno 205、Adreno 220、Adreno 320等
    * 代表作  ：HTC G10、G14，小米1、2等

* ARM的Mali系列
    * 代表型号：Mali-400、Mali-T604等
    * 代表作  ：三星Galaxy SII、Galaxy SIII、Galaxy Note1、Galaxy Note2(亚版)等

* nVIDIA(英伟达)的Tegra系列
    * 代表型号：nVIDIA Tegra2、nVIDIA Tegra3等
    * 代表作  ：Google Nexus 7，HTC One X等

* <a href="http://blog.csdn.net/wanglang3081/article/details/8869589">什么是压缩纹理，如何使用?</a>

# 压缩纹理的必要性

* 图像文件格式和纹理格式的区别
    1. 常用的图像文件格式有BMP，TGA，JPG，GIF，PNG等
    2. 常用的纹理格式有R5G6B5，A4R4G4B4，A1R5G5B5，R8G8B8, A8R8G8B8，RGBA4444，RGB888，RGB565，RGBA5551等
        * R5G6B5 每个像素占用2个字节
        * A4R4G4B4 每个像素占用2个字节
        * A1R5G5B5 每个像素占用2个字节
        * RGBA8888 每个像素4字节，RGBA通道各占用8位
        * A8R8G8B8 每个像素占用4个字节
        * RGBA4444 每个像素2字节，RGBA通道各占用4位
        * RGB888 每个像素3字节，RGB通道各占用8位，无透明通道
        * RGB565 每个像素2字节，RGB通道各占用5/6/5位，无透明通道
        * RGBA5551 每个像素2字节，RGB通道各占用5位，透明通道1位，所以要么完全透明要么不透明
* 批注
    1. Android平台默认支持格式：JPEG、PNG、GIF、BMP 和 WebP
    2. iOS平台默认支持格式：JPEG、JPEG2000、PNG、GIF、BMP、ICO、TIFF、PICT、APNG、SVG、RAW

* 详细区分解读
    * 文件格式是图像为了存储信息而使用的对信息的特殊编码方式，它存储在磁盘中，或者内存中，但是并不能被GPU所识别，因为以向量计算见长的GPU对于这些复杂的计算无能为力。这些文件格式当被游戏读入后，还是需要经过CPU解压成R5G6B5，A4R4G4B4，A1R5G5B5，R8G8B8, A8R8G8B8等像素格式，再传送到GPU端进行使用
    * 纹理格式是能被GPU所识别的像素格式，能被快速寻址并采样

* OpenGL ES 2.0支持以上提到的R5G6B5，A4R4G4B4，A1R5G5B5，R8G8B8，A8R8G8B8等纹理格式<br/>
![alt](https://images.cnitblog.com/blog/16411/201302/04151230-6caff70f05cb4028a26ac0aa7e4e019f.jpg)

* 既然主流的图像文件格式移动端设备大部分都可以支持，为什么还需要纹理压缩？
    1. JPEG不支持透明度 
        * 一般在游戏中，有很多透明物件，而JPEG不支持透明度，所以没法使用

    2. PNG不支持随机读取像素
        * PNG格式支持透明度，但是因为PNG的压缩算法是根据图片整体进行压缩(比如采用霍夫曼编码)，像素和像素之间存在依赖关系，无法直接实现单个像素级别的解析，这就没办法发挥显卡的优势

    3. 无法减小显存占用
        * 无论是JPEG或者PNG 的图片最终在显卡解码之后，都是RGBA纹理，无法减小显存的占用(256×256像素的图片，虽然文件格式、磁盘占用、内存占用大小不一样，但都是占用256Kb的显存空间，性能消耗大)

    4. 纹理格式需求
        * 正是因为传统的图片并没有考虑显卡的这种特性，所以很难满足三维应用中的要求<br/>
        要满足显卡能使用的纹理格式，应该具有以下特点：
            1. 解析速度
                * 在纹理操作中，读取纹理数据是关键步骤，所以解码速度至关重要，这一点是最应该考虑的

            2. 随机读取数据
                * 能快速的随机读取任意像素，因为显卡中很多操作都是针对单个像素执行的，所以这一点也很重要

            3. 压缩率和纹理质量
                * 既要保证一个不错的压缩效果，也要把纹理损失控制在一定范围内

            4. 压缩速度
                * 通常纹理压缩在渲染前已经提前准备好，所以如果压缩的速度比解析速度慢，也是可以接受的。

        * <font color="red">说明：Unity在导入图片后，编辑器会卡住弹出一个进度条窗口，就是使用原图进行纹理压缩然后生成新的图片文件的过程。生成的文件的位置,你可以根据导入图片对应的.meta文件内的guid,去项目中的Library文件夹中查找，Unity打包图片的Assetbundle其实是根据这个文件来打包的，这已经不是你导入的那张原图</font>

# 常见的压缩纹理格式
* windows端
    * DXT纹理压缩格式 来源于S3(Silicon & Software Systems)公司提出的S3TC(S3 Texture Compression)，基本思想是把4x4的像素块压缩成一个64或128位的数据块，是有损压缩方式。DXT1-DXT5是S3TC算法的五种变化，用于各种Windows设备。
        * DXT1 格式主要适用于不具透明度的贴图或仅具一位Alpha的贴图（非完全透明则即完全不透明），对于完全RGB565格式的贴图，DXT1具有4：1的压缩比，即平均每个像素颜色占4位，虽然压缩比并不是很好，但是DXT的特性使得它更适合用于实时游戏之中。

            * DXT1将每4×4个像素块视为一个压缩单位，压缩后的4×4个像素块占用64位，其中有2个16位的RGB颜色和16个2位索引，格式描绘如下图所示： <br/>
            ![alt](https://img-blog.csdn.net/20150410153630066)
            
            * DXT1中的两个RGB颜色负责表示所在压缩的4×4像素块中颜色的两个极端值，然后通过线性插值我们可以再计算出两个中间颜色值，而16个2位索引则表明了这4×4个像素块所在像素的颜色值，2位可以表示4种状态，刚好可以完整表示color_0，color_1以及我们通过插值计算出的中间颜色值color_2和color_3，而对于具有一位Alpha的贴图，则只计算一个中间颜色值，color_3用来表示完全透明。

            * 对于如何判断DXT1格式是表示不透明还是具有1位alpha的贴图，则是通过两个颜色值color_0和color_1来实现的，如果color_0的数值大于color_1则表示贴图是完全不透明的，反之则表示具有一位透明信息。因为只有一位 Alpha 信息，所以只能表示透明或不透明，因此DXT1的透明其实是一种镂空，利用网孔达到的透明效果，我们一般对画面质量要求不高并且不需要透明信息的图片使用这种格式。
* 移动端基于OpenGL ES的压缩纹理有常见的如下几种实现：
    1. ETC（Ericsson texture compression)纹理压缩是由 Khronos 支持的开放标准，在移动平台中广泛采用。 <br/>它是一种为感知质量设计的有损算法，其依据是人眼对亮度改变的反应要高于色度改变。<br/>所有的Android设备所支持
        * <a herf="https://www.khronos.org/registry/OpenGL/extensions/OES/OES_compressed_ETC1_RGB8_texture.txt">ETC原理 </a>
        * OpenGL ES的扩展名为: GL_OES_compressed_ETC1_RGB8_texture
        * ETC1
            * 从OpenGL ES2.0 标准中要求必须支持的一种压缩纹理格式.
            * 优点：标准支持，通用性好。<br/>缺点：只有rgb,没有a通道,不支持透明通道，使用场合大大受限
            * 原理如下：<br/>
            把一个4x4的像素单元组压成一个64位的数据块。4x4的像素组先被水平或垂直分割成2个4x2的组，每一半组有1个基础颜色（分别是RGB444/RGB444或RGB555/RGB333格式）、1个4位的亮度索引、8个2位像素索引。每个像素的颜色等于基础颜色加上索引指向的亮度范围<br/>
            比如对于某一个半组： 
                1. 12位的基础颜色是RGB(0, 34, 255)； 
                2. 4位的亮度索引从亮度表中选择亮度补充，亮度表有16个，下表是0-7，8-15是0-7的2倍。<br/>
            ![alt](https://img-blog.csdn.net/20150410173541224)<br/>
            亮度索引1对应(-12, -4, 4, 12), 2位的像素索引是0，所以亮度补充是-12。由此可以得到此像素的颜色值是(0-12, 34-12, 255-12)，也即(0, 22, 243)
            
            * 如果想用ETC1格式压缩但又想透明怎么办？
                + 用另外一张纹理，也是ETC1 用rgb中的一个通道保存alpha值，在shader中组合起来。就实现了ETC1格式支持透了
            * 当加载压缩纹理时，< internal format > 参数支持如下格式：GL_ETC1_RGB8_OES(RGB，每个像素0.5个字节)

        *  ETC2 (从OpenGL ES3.0 开始支持ETC2纹理压缩)，是ETC1的扩张，向后兼容ETC1，对RGB的压缩质量更好，并且支持透明通道

    2. PVRTC (PowerVR texture compression)
        * 支持的GPU为Imagination Technologies的PowerVR SGX系列。
        *  OpenGL ES的扩展名为: GL_IMG_texture_compression_pvrtc。
        * 当加载压缩纹理时，< internal format >参数支持如下几种格式：
            * GL_COMPRESSED_RGB_PVRTC_4BPPV1_IMG (RGB，每个像素0.5个字节)
            * GL_COMPRESSED_RGB_PVRTC_2BPPV1_IMG (RGB，每个像素0.25个字节)
            * GL_COMPRESSED_RGBA_PVRTC_4BPPV1_IMG (RGBA，每个像素0.5个字节)
            * GL_COMPRESSED_RGBA_PVRTC_2BPPV1_IMG (RGBA，每个像素0.25个字节)
        * 缺点是需要图片的长宽相等而且为2的幂次方(针对这种情况，可以考虑把图片打包在一张2的幂次方图集内）

    3. ATITC (ATI texture compression)
        * 支持的GPU为Qualcomm的Adreno系列。
        * 支持的OpenGL ES扩展名为: GL_ATI_texture_compression_atitc。
        * 当加载压缩纹理时，<internal format>参数支持如下类型的纹理：
            * GL_ATC_RGB_AMD (RGB，每个像素0.5个字节)
            * GL_ATC_RGBA_EXPLICIT_ALPHA_AMD (RGBA，每个像素1个字节)
            * GL_ATC_RGBA_INTERPOLATED_ALPHA_AMD (RGBA，每个像素1个字节)

    4. S3TC (S3 texture compression)也被称为DXTC，在PC上广泛被使用，但是在移动设备上还是属于新鲜事物。支持的GPU为NVIDIA Tegra系列。
        * OpenGL ES扩展名为:
        * GL_EXT_texture_compression_dxt1和GL_EXT_texture_compression_s3tc。
        * 当加载压缩纹理时，<internal format>的参数有如下几种格式：
            * GL_COMPRESSED_RGB_S3TC_DXT1 (RGB，每个像素0.5个字节)
            * GL_COMPRESSED_RGBA_S3TC_DXT1 (RGBA，每个像素0.5个字节)
            * GL_COMPRESSED_RGBA_S3TC_DXT3 (RGBA，每个像素1个字节)
            * GL_COMPRESSED_RGBA_S3TC_DXT5 (RGBA，每个像素1个字节)
        
    5. ASTC纹理压缩格式
        * ASTC中ARM研发的一种较新的贴图压缩格式<br/>
        从IOS9(A8架构，现在都iOS12了)开始支持ASTC压缩格式 ，相对于PVRTC2/4而言，ASTC(4X4)的压缩比会增加到0.25，不过显示效果也会好很多，而且不要求图片长宽相等且为2的幂次方。<br/>
        Android设备也支持，可以说，ASTC是目前最好的纹理压缩格式，默认首选。(由于这是最新压缩格式，所以可能存在部分机型不适配或者软件解压的时候存在bug)

* 当然还有其他的类型 （比如：RGBA32、RGBA16、Alpha8）

# 图片在Unity中的处理

1. 在实际游戏引擎内(以Unity为例)，图片显示流程大概是：原图 —> 纹理压缩(ETC/ETC2等)—>OpenGL ES3—>显卡—>游戏内显示

2. 而一张图片在游戏引擎中运行时占用的显存大小，与这种图片本身占用磁盘大小无关，只和图片的宽和高相关

3. 图片导入到Unity引擎中，之所以很慢，是因为在进行纹理压缩编码生成新的文件，这一步很耗时间,
    * <font color="red"> Android 平台下 如果原图带有a通道会自动转换成为Etc2压缩格式 </font>
    * <font color="red"> 当Graphic Api框架修改时不会自动转为当前APi框架支持的压缩格式，而是以当前的压缩格式打进AB包，在加载时依赖于Fallback机制还原成原图格式 </font>

4. AssetBundle打包图片，不是打包原图，而是针对你对图片设置的纹理压缩格式而生成的新的文件打包，所以ab文件的大小也和原图本身占用磁盘大小无关

5. AssetBundle文件的大小与加载成正比关系，同时，纹理压缩格式的解码时间也有差异(差异不是很大)

6. 平台的最佳纹理压缩格式是:astc_rgba_4x4 它兼顾了成像质量,内存占用，ab大小，解码时间，跨平台等因素

