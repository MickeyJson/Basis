**Lua Table底层实现详解**

* lua使用table的单一结构，既可以做array，又可以成为hash，链表，树等结构，是一种简洁高效的使用形式。
  即使是对虚拟机来说，访问表项也是由底层自动统一操作的，因而上层业务不必考虑这种区别。
  * 表会根据其自身的内容自动动态地使用这两个部分：
    * 数组部分试图保存所有那些键介于1 和某个上限n之间的值。
    * 非整数键和超过数组范围n 的整数键对应的值将被存入散列表部分。
  
* 底层实现是一个“结构体”
```
 typedef struct Table 
 {
    CommonHeader;
    lu_byte flags;  /* 1<<p means tagmethod(p) is not present */
    lu_byte lsizenode;  /* log2 of size of `node' array */
    struct Table *metatable;
    TValue *array;  /* array part */
    Node *node;
    Node *lastfree;  /* any free position is before this position */
    GCObject *gclist;
    int sizearray;  /* size of `array' array */
  }
```
  * 内部变量详解
    * array： lua表的数组部分起始位置的指针。
    * node：lua表hash数组的起始位置的指针。
    * sizearray：lua表数组部分的指针。
  * 根据数据结构看：table的数组部分和hash部分是分别存储的。
    * 什么样的数据会进入数组部分，什么样的数据会进入hash部分？
      * [0]是一定进入hash，[string]一定是hash的，[int]可能是array，也可能是hash
      * 所以在内部由一个算法来决定什么数据进入数组部分，什么数据进入hash部分
      * 决定进入数组还是hash算法：数组在每一个2次方位置,其容纳的元素数量都超过了该范围的50%,能达到这个目标的话,那么Lua认为这个数组范围就发挥了最大的效率。
    ```
      举个小例子来说明一下：
      local p = {[0]=1,[1]=2,[2]=2,[4]=3}
      print(#p)  
      local px = {[0]=1,[1]=2,[2]=2,[5]=3}
      print(#px)
      输出结果为： 4  2
    ```
      * 算法简说：除去0，index 1， 2，4 一共3个元素。
      * 算法 : 2^i < 3，i = 2, max_idx = 2^2 =4, index大于4的进入hash。
  * 内部算法(ltable.c)
    ```
    static int computesizes (int nums[], int *narray) {
     int i;
     int twotoi;  /* 2^i */
     int a = 0;  /* number of elements smaller than 2^i */
     int na = 0;  /* number of elements to go to array part */
     int n = 0;  /* optimal size for array part */
     for (i = 0, twotoi = 1; twotoi/2 < *narray; i++, twotoi *= 2) {
       if (nums[i] > 0) {
         a += nums[i];
         if (a > twotoi/2) {  /* more than half elements present? */
           n = twotoi;  /* optimal size (till now) */
           na = a;  /* all elements smaller than n will go to array part */
         }
       }
       if (a == *narray) break;  /* all elements already counted */
     }
     *narray = n;
     lua_assert(*narray/2 <= na && na <= *narray);
     return na;
    }
    nums[]: 构建的一个数组，根据index的大小，添加计数对应的位置，保证 2^(i-1) < index < 2^i。
    narray：数组的size
    ```

  
  * 另外： 对于数组的定义
    ```
    local px = {[0]=1,[1]=2,[2]=2,[5]=3}  --hash
    local px = {1,2,2,3}  --array
    ```
  * 当对于数组关键节（i = 2^n）点进行定义，修改的时候才会触发computesizes，重新进行分配。
  * 新的数组部分的大小是满足以下条件：
    * 1到n 之间至少一半的空间会被利用（避免像稀疏数组一样浪费空间）；
    * 并且n/2+1到n 之间的空间至少有一个空间被利用（避免n/2 个空间就能容纳所有数据时申请n 个空间而造成浪费）。
    * 当新的大小计算出来后，Lua 为数组部分重新申请空间，并将原来的数据存入新的空间。

* 这种混合型结构有两个优点:
  * 存取整数键的值很快，因为无需计算散列值。
  * 第二，也是更重要的，相比于将其数据存入散列表部分，数组部分大概只占用一半的空间，因为在数组部分，键是隐含的，而在散列表部分则不是。
* 结论就是，如果表被当作数组用，只要其整数键是紧凑的（非稀疏的），那么它就具有数组的性能，而且无需承担散列表部分的时间和空间开销，因为这种情况下散列表部分根本就不存在。相反，如果表被当作关联数组用，而不是当数组用，那么数组部分就可能不存在。这种内存空间的节省很重要，因为在Lua 程序中包括我们开发当中，常常创建许多很小的表。
