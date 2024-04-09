import io

wordSet = set()

with open("./wordlist.txt","r") as fp:
    for line in fp.readlines():
        wordSet.add(line.upper())

wordList = list(wordSet)
wordList.sort()

with open("./wordlist.txt","w") as fp:
    fp.writelines(wordList)