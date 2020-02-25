function treeToArray(
  data,
  expandAll,
  parent = null,
  level = null
) {
    let tmp = [];
    Array.from(data).forEach(function (record, index) {
        if (record._expanded === undefined) {
            window.Vue.set(record, "_expanded", false)
    }
    let _level = 1;
    if (level !== undefined && level !== null) {
      _level = level + 1;
    }
   window.Vue.set(record, "_level", _level)
    // 如果有父元素
    if (parent) {
      window.Vue.set(record, "parent", parent)
    }
        tmp.push(record);
    if (record.children && record.children.length > 0) {
      const children = treeToArray(record.children, expandAll, record, _level);
      tmp = tmp.concat(children);
    }
  });
  return tmp;
}
function insertTreeChildren(treearray, recordOfTree, childrenData) {
    var level = 1;
    if (recordOfTree != null)
        level = recordOfTree._level;
    var childrenTree = treeToArray(childrenData, true, recordOfTree, level);
    var pos = treearray.indexOf(recordOfTree) + 1;
    recordOfTree.children = childrenTree;
    treearray.unshift(pos, 0);
    Array.prototype.splice.apply(treearray, childrenTree);
    return treearray;
}