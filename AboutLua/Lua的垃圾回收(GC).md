# Lua的垃圾回收

# <font face="微软雅黑" size = 5>前序</font>
看过很多lua的相关学习系列和其中特性详解，也买了本书学习，但是关于Lua的垃圾回收机制的详细介绍有之甚少。<br/>
曾经翻阅到一篇介绍，个人觉得非常详细，下面就来看看

# <font face="微软雅黑" size = 5>GC类型</font>
研究 Lua GC 的第一个收获是 GC 方式的细致分类。
* Lua GC属于root-tracing这个大类

* root 指 reachability 的源头，一般指全局变量 和当前 thread 的 stack

* tracing 指通过对象之间的引用关系检查对象的 reachability，以是否 reachable 作为回收对象的标准。

* Root-tracing GC 一般采用 <font color="red">mark-and-sweep</font> 策略

* 在 trace 阶段给所有 reachable 对象打一个 mark，然后进入 sweep 阶段，将没有 mark 的对象回收。

* 最简单的实现是把 trace 和 sweep 两个阶段整个作为原子化过程，执行中不允许虚拟机执行 OP_CODE，这就是 stop-the-world GC。

* 更为复杂的策略是把内存中的对象按照生命周期长度分成「代 (generation)」，每次仅对一代对象进行 mark-and-sweep 操作。而且对每代进行操作的频度不同，叫做 generational GC。如果设计合理，这种策略可以及时回收临时变量又避免了对生命周期很长的对象进行过多不必要的 trace 和 mark。

* 为了实现的简洁，Lua 直到 5.1 都没有 generational GC。5.2 版本实现了这个策略，但是缺省处于关闭状态，而且设计者一再声明是一个实验性的功能，将来可能移除。

* 目前 Lua 采取的策略是把 trace 和 sweep 两个阶段分成很多小片段，在执行各个片段之间允许虚拟机执行 OP_CODE。其代价是 GC 无法在一个周期中识别出所有 unreachable 对象，导致部分 unreachable 对象只有到下次「GC 周期」才能被回收。这种把 trace/sweep 分成多个片段的方式称为 incremental GC。


# <font face="微软雅黑" size = 5>周期和步骤</font>
<font face="微软雅黑" size = 3.5>1. 既然提到了「GC 周期」，就先把一个周期的完整步骤列出来：</font>
* Pause－设置 GC 的基本初始状态，特别是 root-tracing 中的 root；

* Propagate－主要实现 mark-and-sweep 中的 trace 阶段；

* Atomic－实现从 trace 阶段转到 sweep 阶段中不能被打断的原子化部分；

* Sweep-string－实现 sweep 阶段中为 string 优化的部分；

* Sweep-userdata－实现 sweep 阶段中对 userdata 的处理；

* Sweep－Sweep 阶段的主要实现。

<font face="微软雅黑" size = 3.5>这些步骤的入口和跳转的逻辑在 singlestep() 函数中实现。</font>



<font face="微软雅黑" size = 3.5>2. 再说说 collectable value 的「颜色」概念。</font>
* Lua 中需要被 GC 回收的 value 被称为 <font color="red">collectable value</font>，其共有属性由 struct GCheader 实现，其中 <font color="red">field marked</font> 表示一个<font color="red">value 的「颜色」。</font>

* Field marked 的 bit 0 和 bit 1 表示颜色是否为 white，bit 3 表示颜色是否为 black。但是一个 value 的颜色并不仅仅由 marked 决定。

* 为了不引起混淆，我们把仅仅由 field marked 决定的颜色称为「marked 颜色」，把所有因素共同决定的颜色称为 value 的「颜色状态」。

* Lua 虚拟机的全局标志 global_State::currentwhite 用来解释 marked 的 bit 0 和 bit 1 哪个表示 current-white，另一个 bit 表示 other-white。

<font face="微软雅黑" size = 3.5>新创建 value 初始被置为「current-white 状态」。</font>

# <font face="微软雅黑" size = 5>Lua GC步骤详解</font>
* Lua GC 的 trace 阶段对应于「propagate 步骤」，每次执行时搜索到的 reachable value 的 marked 颜色被设为「black」。这些 value 中有一部分 比如 table 和 function —— 可以再引用其它 value  ( table 通过 key-value，function 通过 upvalue ) 。一个可以引用其它 value 的 reachable value 的 marked 颜色刚刚变为 black 之后，它自己会被放到一个称为 gray-list 的链表中。这种 marked 颜色为 black 且处于 gray-list 中的 value 被视为「gray 状态」。

    * 在一次 propagate 「片段」中，Lua GC 会把片段开始前就已经存在于 gray-list 中的全部或者一部分 gray value 取出 (使它们成为真正的「black 状态」)，把它们直接引用的 value 置为 black 状态 (如果是简单的不会引用其它 value 的类型，比如 string) 或者 gray 状态。一旦 value 处于 black 状态 ( marked 颜色为 black 并且不在 gray-list 中) ，在当前 GC 周期中就不再被检查。具体代码中上述这些工作由两个函数实现：

        * propagatemark() --> 这个函数代表 root-tracing GC 中的 trace

            * 将 gray-list 头部的 value 取出变成 black 状态，把它引用的其它 value 变成 gray 状态或 black 状态。

        * markvalue() / markobject()  --> 它实现的概念是 mark-and-sweep 中的 mark。

            * 接受一个表示 value 的参数，把这个 value 变成 gray 或者 black 状态。

    * 这两个函数相互配合实现 trace 过程：propagatemark() 直接调用 markvalue() / markobject() 来扩散 value 的 black 状态，markvalue() / markobject() 向 gray-list 中添加value 来影响后续的 propagatemark() 调用。函数 propagatemark() 每次进行 trace 起点是 gray-list。

    * 上文提到过 root-tracing GC 的 root 是全局变量和各个 thread stack。

    * 关联这种「root」到 gray-list 的任务由 pause 步骤完成。Pause 步骤的实现主要在函数 restartcollection()中，代码如下：
        ``````
        static void restartcollection (global_State *g) {
            g->gray = g->grayagain = NULL;
            g->weak = g->allweak = g->ephemeron = NULL;
            markobject(g, g->mainthread);
            markvalue(g, &g->l_registry);
            markmt(g);
            markbeingfnz(g);  /* mark any finalizing object left from previous cycle */
        }
        ``````

        * 其中的 markobject(g, g->mainthread) 将 main-thread 放入 gray-list (即代码中的 g->gray)。在 Lua 中，全局变量存储在 main-thread 的 upvalue table _ENV 中。

        * 把 main-thread 放入 gray-list 就完成了对 root 的准备工作 —— 把全局变量，thread stack 与 gray-list 关联起来。

* 至此我们简单讨论了 value 的颜色状态以及 pause/propagate 两个步骤。

* 接下来看其它 GC 步骤。
    
    * 函数 propagatemark() 不断从 gray-list 中取出 value，期间 markvalue() / markobject() 也会添加一些 value，不过最终 gray-list 会被取空，这时 Lua GC 进入 atomic 步骤。
        * Atomic 步骤主要由两个函数实现：atomic() 和 entersweep() 。下面是 atomic() 函数的代码。在这个函数中 GC 把虚拟机中的所有 thread 以及一些已经处于 black 状态的 value 重新置于 gray 状态 (见 atomic() 函数中对 retraversegrays() 函数的调用及其前后的代码) ，随后进行数次不会被打断的 non-incremental trace (调用 propagateall() )。这几次临近 sweep 阶段前最后的 non-incremental trace —— 也可以叫做 atomic trace 确保所有 current-white value 都是 unreachable value。
        ``````
        static l_mem atomic (lua_State *L) {
            global_State *g = G(L);
            l_mem work;
            GCObject *origweak, *origall;
            GCObject *grayagain = g->grayagain;  /* save original list */
            lua_assert(g->ephemeron == NULL && g->weak == NULL);
            lua_assert(!iswhite(g->mainthread));
            g->gcstate = GCSinsideatomic;
            g->GCmemtrav = 0;  /* start counting work */
            markobject(g, L);  /* mark running thread */
            /* registry and global metatables may be changed by API */
            markvalue(g, &g->l_registry);
            markmt(g);  /* mark global metatables */
            /* remark occasional upvalues of (maybe) dead threads */
            remarkupvals(g);
            propagateall(g);  /* propagate changes */
            work = g->GCmemtrav;  /* stop counting (do not recount 'grayagain') */
            g->gray = grayagain;
            propagateall(g);  /* traverse 'grayagain' list */
            g->GCmemtrav = 0;  /* restart counting */
            convergeephemerons(g);
            /* at this point, all strongly accessible objects are marked. */
            /* Clear values from weak tables, before checking finalizers */
            clearvalues(g, g->weak, NULL);
            clearvalues(g, g->allweak, NULL);
            origweak = g->weak; origall = g->allweak;
            work += g->GCmemtrav;  /* stop counting (objects being finalized) */
            separatetobefnz(g, 0);  /* separate objects to be finalized */
            g->gcfinnum = 1;  /* there may be objects to be finalized */
            markbeingfnz(g);  /* mark objects that will be finalized */
            propagateall(g);  /* remark, to propagate 'resurrection' */
            g->GCmemtrav = 0;  /* restart counting */
            convergeephemerons(g);
            /* at this point, all resurrected objects are marked. */
            /* remove dead objects from weak tables */
            clearkeys(g, g->ephemeron, NULL);  /* clear keys from all ephemeron tables */
            clearkeys(g, g->allweak, NULL);  /* clear keys from all 'allweak' tables */
            /* clear values from resurrected weak tables */
            clearvalues(g, g->weak, origweak);
            clearvalues(g, g->allweak, origall);
            luaS_clearcache(g);
            g->currentwhite = cast_byte(otherwhite(g));  /* flip current white */
            work += g->GCmemtrav;  /* complete counting */
            return work;  /* estimate of memory marked by 'atomic' */
        }
        ``````

    * 即使到了这一步，Lua GC 也并未完全把 value 的 reachability 和颜色状态严格对应起来。
    * Atomic 步骤确保的只是从 current-white 到 unreachable 的单方向对应。反方向并不成立，即 unreachable value 并不一定都处于 current-white 状态，因为有些 black value 在被 mark 之后才变成 unreachable 状态。这不影响 GC 的正确性，只是这些 unreachable value 要等到下个 GC 周期才能得到回收。这就是 incremental GC 的取舍：每次中断 OP_CODE 运行的时间都不长，但是一个「GC 周期」不能确保所有垃圾完全回收干净。

    * Atomic 步骤中的 trace 过程完成之后，atomic() 函数会修改 g->currentwhite (上述代码块第三行) 。 这就是上文说过的用来解释 collectable value 的 marked field 中 bit 0 和 bit 1 意义的 flag。这次修改令所有 current-white value 立即转到 other-white 状态。至此，为 sweep 阶段所做的准备已经基本完成。Sweep 阶段的主要任务是回收 other-white 状态的 value，又称为 dead value。

    ``````
    static void separatetobefnz (global_State *g, int all) {
        GCObject *curr;
        GCObject **p = &g->finobj;
        GCObject **lastnext = findlast(&g->tobefnz);
        while ((curr = *p) != NULL) {  /* traverse all finalizable objects */
            lua_assert(tofinalize(curr));
            if (!(iswhite(curr) || all))  /* not being collected? */
            p = &curr->next;  /* don't bother with it */
            else {
            *p = curr->next;  /* remove 'curr' from 'finobj' list */
            curr->next = *lastnext;  /* link at the end of 'tobefnz' list */
            *lastnext = curr;
            lastnext = &curr->next;
            }
        }
    }
    ``````
* 这里还有一个例外：atomic() 函数中调用的 separatetobefnz() 函数把所有被 __gc mark过的 dead value 放到 g->tobefnz 链表中。<br/><br/>Sweep 阶段不回收这些 value，而是留待下次 GC 周期一开始调用它们的 __gc meta-method，之后把它们作为普通的没有 __gc mark 的 value 常规处理。<br/>这是因为 dead value 的 __gc meta-method 有可能把它自己重新赋给 其它变量，使其恢复 reachable 状态，这种现象叫做「resurrection」。Lua GC 不能用 mark-and-sweep 检测 resurrection 现象，否则 mark-and-sweep 算法会变成无限递归过程。<br/><br/>Lua GC 采用的方法虽然 ad-hoc 但是也算取舍得当：1.定义了「__gc mark」这个概念，缩小了 __gc meta-method 起作用的范围 。2.在当前 GC 周期中不回收 __gc marked value。<br/>在下一个 GC 周期中这些 value 失去「__gc marked」资格 (除非它们再次在代码中被显式的 __gc mark) 后再做普通处理。<br/>一开始说过，很多讲 Lua 虚拟机的资料建议尽量推迟对 GC 的研究，也是因为 Lua GC 在简单的 mark-and-sweep 算法上添加了很多特殊情况的处理。

* 如果在 atomic 步骤执行的 non-incremental trace 耗时太长，就会影响 Lua 虚拟机执行 OP_CODE 的性能。因为之前的 propagate 步骤已让大部分 reachable value 处于 black 状态。<br/>此时需要 trace 的只是各个 thread 被置为 black 状态之后新创建的 value。<br/><br/>这时有一个疑问，因为 thread 被置为 black 的时间点在一次 GC 周期中并不是非常靠后，那么 atomic trace 处理的 value 是否会非常多？<br/><br/>有两个因素保证这个担心是不必要的：
    * 在 atomic 阶段真正要被处理的 value 实际上少于「thread 被置为 black 状态之后新创建的 value」。<br/>因为在 thread 被置为 black 状态之后创建的 value 中有一部分还被 gray-list 中的其它 value 引用，这样的 value 在 propagate 阶段就会被 trace。

    * 那些在 thread 被置为 black 之后新创建的，而且没有被其它 gray value 引用的 value，大多仅被 stack 上的 local 变量引用。<br/>到了 atomic 阶段，这些 local 变量中很多已经离开了自己声明的 block，相应的 value 处于 unreachable 状态。所以 atomic trace 不会处理它们。<br/>到了后面的 sweep 阶段，因为它们处于 other-white 状态，会被回收。

    * 接下来，atomic 步骤的 entersweep() 函数会把所有 collectable value 放入几个 sweep-list 链表。<br/>然后 sweep 阶段的几个步骤会遍历这些链表，回收其中的 dead value，把其中的 black value 变为 current-white 状态留待下个 GC 周期处理。<br/>因为 value 颜色状态的复杂逻辑已经在 trace 阶段处理完毕，sweep 阶段的逻辑比较简单，只需要注意一次回收的时间不能打断 OP_CODE 的运行太久。

# <font face="微软雅黑" size = 5>内存分配</font>
上面讨论的主要是内存的 trace 和 mark，简单的提了一下 sweep。还有一个方面没有涉及，就是在 GC 的每次运行片段之间 Lua OP_CODE 的执行所创建的新 value 如何影响 GC 的状态。

上面只简单提了「新创建的 value 被标记为 current-white」。实际上，current-white value 总要有其它 value (称为「referrer」) 去引用它 (否则就让它一直 current-white 下去，直到 atomic 步骤变成 other-white 然后被 sweep 回收就好了) ，还可能随时增加新的 referrer。如果这些 referrer 是 current-white 或者 gray 状态，处理起来比较简单：只要等到 GC 去 trace 这些 referrer 就好。可对于 black 状态的 referrer，如果不做额外处理，GC 就不会再次 trace 它们，那么它们后来引用的 current-white value 就不能反应正确的 reachability 状态。

所以处于 black 状态的 referrer 和其它的 current-white value 建立新的引用关系时，涉及两种处理：

> 如果该 referrer 引用其它 value 的关系比较复杂，Lua 虚拟机会调用 luaC_barrierback()，把这个 referrer 加回到 gray-list 中。<br/>
> 如果该 referrer 引用其它 value 的关系比较简单，Lua 虚拟机会调用 luaC_barrier()，把新被引用的 current-white value 置为 gray 或者 black 状态。<br/>

这两个方法 ——  luaC_barrierback() 和 luaC_barrier() 是 Lua 虚拟机的其它部分和 GC 进行显式交互的主要接口。

此时还遗留了一个问题：<font color="red">local 变量和 thread。</font><br/><br/>理论上来说，一个新创建 value 如果只赋给 local 变量，那么它是被当前 thread 通过 Lua stack 引用，也应调用上面的两个 barrier 函数之一进行处理，但前面说过 local 变量的生命周期不长，不值得每次给它们赋值时都把当前 thread 重新放回 gray-list 或者把赋值的 current-white value 立即置为 gray 状态。<br/><br/>Lua GC 对这个问题的处理是给 thread 一个特殊的颜色状态：当 thread 从 gray-list 中被取出的时候，它被放到一个 gray-again-list 链表中，该链表在 atomic 步骤的 retraversegrays() 被放回 gray-list 由 atomic trace 处理。

由此可以看出 Lua GC 对临时变量的处理节省了 CPU，但是大大增加了它们在内存中存活的时间。这也是 Lua 在有大量临时变量的应用中占用内存比较多的原因。<br/><br/>
同时，过多的临时变量没有及时没识别出来，最后势必也会加重 sweep 阶段的 CPU 负担。所以 Lua 目前也在考虑如何利用和改进刚刚加入的 generational GC。

脚注：

从 Lua 5.2 开始，严格意义上 Lua 不再有全局变量。<br/>
全局变量是 top-level chunk 的 upvalue _ENV 的属性。这个细节在后面讨论「pause 步骤」的时候会提到。<br/>
Lua 的术语，相当于 Java 的 byte-code。<br/>
严格地说，这里的「临时变量」是指「只被临时变量引用的对象」。下文有多处描述类似。<br/>
 * 本文把 mark-and-sweep GC 的两个大步骤 —— trace 和 sweep 称为「阶段」。
    * 把 Lua 的 incremental GC 实现的六个状态称为「步骤」。
    * 其中 Lua GC 的最后一个步骤也叫做 sweep，不要和「sweep 阶段」混淆。每个步骤并不是原子化执行，而是分成「片段」多次完成。

* 前文泛指 GC 讨论时用了「对象」这个名词。因为 Lua 不是单纯的面向对象语言，所以后文采用《Programming in Lua》中的术语：
    * 在 Lua 中，赋给变量的东西叫做「value」。
        * value 有九种类型：nil, boolean, number, string, table, function, userdata, thread。<br/><font color="red">其中 string, table, function, userdata, 和 thead 是 collectable value。</font>
* Mark-and-sweep GC 中的 trace 阶段是 mark and propagate mark。<br/>其它类型的 root-tracing GC 的 trace 操作不一定如此，比如 copy-and-sweep GC 的 trace 阶段执行的是拷贝对象。<br/><br/>「__gc mark」是指给一个 value 设置 meta-table 时，后者就包含 __gc meta-method。给一个 value 已有的 meta-table 添加 __gc 方法并不是「__gc mark」，所加的方法也不会被 Lua GC 调用。其它 meta-method 只要在相应的 meta-event 出现时存在，就会被调用,而 __gc 必须在设定 meta-table 时就存在才会被 GC 调用。<br/><br/>
考虑到之前所说，只要 Lua Registry 中的 value 和 meta-table 被 trace 完毕，Lua GC 就会去 trace main-thread。这时只要 main-thread 的 upvalue 和 stack 上的 value 被置为 gray 状态，main-thread 就会完全变 black。唯一引用它们的地方 —— stack，已经 shrink 回不再包含它们的状态。


# <font face="微软雅黑" size = 3>[摘抄至技术奇异点](https://techsingular.net/)</font>
