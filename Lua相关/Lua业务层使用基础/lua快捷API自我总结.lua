function MoneyNumberFormat(value)
    --小数点位数
    local radixPoint = math.pow(10, 1);
    --基础单位值
    local baseNum = 1e4;
    --小于最低值，直接返回
    if value < baseNum then
        return tostring(value);
    end
    --返回值
    local result = '';
    local strArg = {'', '万', '亿', '万亿'};
    local i = math.floor(math.log(value) / math.log(baseNum));
    if i+1 > 4 then
     i = 4;
    end
    result = math.floor(value / math.pow(baseNum, i) * radixPoint) / radixPoint..strArg[i+1];
    return result;
end

function NumberFormat(number,format)
    if format == 0 then 
        -- 三位逗号显示
       local t = tonumber(number)
       local head = t or 0
       local left = 0
       local out = ""
       repeat
            left = math.floor(head%1000)
            head = math.floor(head/1000)
            if out=="" then
                if head==0 then
                    out = string.format("%d",left)
                else
                    out = string.format("%03d",left)
                end
            elseif head>0 then
                out = string.format("%03d,%s",left,out)
            else
                out = string.format("%d,%s",left,out)
            end
       until head==0
       return out
    end
end