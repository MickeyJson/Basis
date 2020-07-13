module("WordTree",package.seeall)

function InitModule()
end

--创建根结点
local m_RootNode = nil
local illegalDataInited = false

--树节点创建
local function CreateNode(char, flag, childs)
	local node = {}
	node.char = char or nil		--字符
	node.flag = flag or 0		--是否结束标志，0：继续，1：结尾
	node.childs = childs or {}	--保存子节点
	node.isleaf = true --childs数量为0则是叶子节点
	return node
end

--节点中查找子节点
local function FindNode(node, char)
	local childs = node.childs
	for i, child in ipairs(childs) do
		if child.char == string.lower(char) then 
			return child
		end
	end
end

--插入节点
local function InsertNode(parent, chars, index)
	local node = FindNode(parent, chars[index])
	if node == nil then
		node = CreateNode(chars[index])
		parent.isleaf = false
		table.insert(parent.childs, node)
	end
	local len = #chars
	if index == len then
		node.flag = 1
	else
		index = index + 1
		if index <= len then
			InsertNode(node, chars, index)
		end
	end
end

--更新字典树
local function UpdateNodes(words)
	for i, v in pairs(words) do
		local chars = string.ToCharArray(v, table.tmpEmptyTable())
		if #chars > 0 then
			InsertNode(m_RootNode, chars, 1)
		end
	end
end

--创建trie树
local function CreatTree()
	if illegalDataInited then return else illegalDataInited = true end
	local t1 = os.clock()
    m_RootNode = CreateNode('root') 
	local _allData = Table_IllegalData.GetAllRowData()
	local worldList = {}
	for k,v in pairs(_allData) do
        if v and v.stringValue and v.stringValue ~= "" then
			table.insert(worldList,v.stringValue)
		end
	end
	UpdateNodes(worldList)
    GameLog.LogError("我的创建树，消耗时间：%s",(os.clock()-t1))
end

--将字符串中敏感字用*替换返回
-- flag == true，替换的 * 的数量 = 敏感词长度；flag == false，默认使用 * 替换
function ReplaceMaskWord(str, flag)
	CreatTree()
	local t1 = os.clock()
	local chars = string.ToCharArray(str,table.tmpEmptyTable())
	local index = 1
	local node = m_RootNode
	local prenode = nil
	local matchs = {}
	local isReplace = false
	local lastMatchLen = nil
	local totalLen = #chars
	local function replace(chars, list, last, flag)
        local stars = ""
		for i=1, last do
			local v = list[i]
			if flag then
				chars[v] = "*"
				isReplace = true
			else
				if isReplace then
					chars[v] = ""
				else
					chars[v] = "*"
					isReplace = true
				end
			end
		end
	end
	while totalLen >= index do
		prenode = node
		node = FindNode(node, chars[index])
		if chars[index] == " " then
			if #matchs then
				table.insert(matchs, index)
				node = prenode
			else
				node = m_RootNode
			end
		elseif node == nil then
			index = index - #matchs
			if lastMatchLen then
				replace(chars, matchs, lastMatchLen, flag)
				index = index + (lastMatchLen - 1)
				lastMatchLen = nil
			else
				isReplace = false
			end
			node = m_RootNode
			matchs = {}
		elseif node.flag == 1 then
			table.insert(matchs, index)
			if node.isleaf or totalLen == index then
				replace(chars, matchs, #matchs, flag)
				lastMatchLen = nil
				matchs = {}
				node = m_RootNode
			else
				lastMatchLen = #matchs
			end
		else
			table.insert(matchs, index)
		end
		index = index + 1
	end
	local str = ''
	for i, v in ipairs(chars) do
		str = str..v
	end
    GameLog.LogError("我的替换，消耗时间：%s",(os.clock()-t1))
	return str
end

--字符串中是否含有敏感字
function IsContainMaskWord(str)
	CreatTree()
	local chars = string.ToCharArray(str,table.tmpEmptyTable())
	local index = 1
	local node = m_RootNode
	local masks = {}
	while #chars >= index do
		node = FindNode(node, chars[index])
		if node == nil then
			index = index - #masks 
			node = m_RootNode
			masks = {}
		elseif node.flag == 1 then
			return true
		else
			table.insert(masks,index)
		end
		index = index + 1
	end
	return false
end


return WordTree