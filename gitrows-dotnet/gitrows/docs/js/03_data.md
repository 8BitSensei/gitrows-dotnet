
# Working with Data

GitRows implements a full CRUD interface to you data so you can create, read, update, and delete information in your repository data files.

## get(path[, *object* filter, *string* method = 'fetch' | 'pull')
> requires `token` for **private repos only**

To read the complete data set from a `.json` or `.csv` file you pass the file path to the `.get()` method, which has the basic structure of

```
@namespace/owner/repository:branch/directory/file(.json|.csv)
```

Alternatively you can copy the file url form your browser and pass this instead. Read more about the path structure and how to troubleshoot in the [Path section](https://gitrows.com/docs/js/path) of GitRows' documentation.

```js
const path = '@github/gitrows/data/iris.json';

gitrows.get(path)
 .then( (data) => {
  //handle (Array/Object)data
  console.log(data);
 })
 .catch( (error) => {
  //handle error, which has the format (Object){code:http_status_code,description='http_status_description'}
 });
```

The `get` method accepts as a second argument a filter object which can be used with filtering and aggregation operators. Learn more about the possibilities in the [Filters section](https://gitrows.com/docs/js/filters).

**For reading a file from a private repo you must set your username and token (see put() for more details). Please note that its impossible to decide from the returned status code if the file is private on GitHub or not, as it will always be 404 by GitHub's policy.**

As a third parameter you can set the mechanism for retrieving data from the repository server. It defaults to `fetch` which is sufficent for most use cases and avoids rate limit issues. However, as `fetch` uses the html `raw` endpoints, e.g. `https://https://raw.githubusercontent.com/`, this may lead to a caching latency of a few seconds. If your use case requires faster access times, try `pull` instead which queries the GitHub or GitLab content APIs.

## put(path, *object* data)
> requires `token`

For adding or updating data (and deleting or creating new data files) you must set your username and an OAuth (personal access) token. Unless you feel particularly adventurous you should **never** do this in a production environment like a website. You can generate a new one in your [GitHub Developer Settings](https://github.com/settings/tokens):

```js
const options = {
 username:"yourUsername",
 token:"yourSecretToken"
};

const data = [
 {
  id:"0003",
  title:"A New Title",
  content:"Some new content"
 },
 {
  id:"0004",
  title:"Another New Title"
 }
];

gitrows.options(options);

gitrows.put(path,data)
 .then((response)=>{
  //handle response, which has the format (Object){code:200,description='OK'}
 })
 .catch((error)=>{
  //handle error, which has the format (Object){code:http_status_code,description='http_status_description'}
 });
```

GitRows accepts the data as an `Array` of `Objects` if you want to add one ore more entries (rows) or a single `Object` for appending one entry (row).

To if you want to enforce consistent data structures set `options({strict:true})`. If true, GitRows will check the columns (keys) used in your datafile and add the missing keys with the default value `NULL` or any other value you set with `options({default:''})`. You can also set the columns as an option with `options({columns:[]})`.

## update(path, *object* data[, *object* filter])
> requires `token`

To update an entry from data you must provide it's `id`, which may either be

* the entry's `id property`, if the data consists of an `Array` of `Objects`, e.g. `[{id:'0001', foo:'bar'}]`
* the `property name`, if the data consists of a single `Object`

**or**

a valid filter expession to update more than one entries and an entry of unknown id.

```js
const filter = {id:'0001'};

const data = {
	foo: "bar"
}

gitrows.update(path,data,filter)
 .then((response)=>{
  //handle response, which has the format (Object){code:202,description='Accepted'} if successful or (Object){code:304,description='Not modified'}
 })
 .catch((error)=>{
  //handle error, which has the format (Object){code:http_status_code,description='http_status_description'}
 });
```

If you use the API style you may also append the `id` to the path and omit the second argument:

```
@namespace/owner/repository:branch/directory/file(.json|.csv)/id
```

## replace(path, *object* data)
> requires `token`

To update a data set that is not representing tabular data but a dictionary, or to swap a complete set, use the replace method

```js

const data = {
	foo: "bar"
}

gitrows.replace(path,data)
 .then((response)=>{
  //handle response, which has the format (Object){code:202,description='Accepted'} if successful or (Object){code:304,description='Not modified'}
 })
 .catch((error)=>{
  //handle error, which has the format (Object){code:http_status_code,description='http_status_description'}
 });
```

## delete(path[, *object* filter])
> requires `token`

To delete an entry from data you must provide it's `id`, which may either be

* the entry's `id property`, if the data consists of an `Array` of `Objects`, e.g. `[{id:'0001', foo:'bar'}]`
* the `property name`, if the data consists of a single `Object`

**or**

a valid filter expession to delete more than one entries.

```js
const filter = {id:'0001'};

gitrows.delete(path,filter)
 .then((response)=>{
  //handle response, which has the format (Object){code:204,description='No content'} if successful or (Object){code:304,description='Not modified'}
 })
 .catch((error)=>{
  //handle error, which has the format (Object){code:http_status_code,description='http_status_description'}
 });
```

If you use the API style you may also append the `id` to the path and omit the second argument:

```
@namespace/owner/repository:branch/directory/file(.json|.csv)/id
```

## types(path[, *object* {'$limit':'number'}])

Returns the columns and their data types which can be one of `string`, `number`, `integer`, `array` or `object`. The data types are detetced from the data set values. To speed detection up you can optionally pass a filter argument with the number of rows to be processed. If mixed values are found within a column, the following take precedence: `string` over `number` over `integer`.

```js
const limit = {'$limit':'10'};

gitrows.types(path,limit)
 .then((response)=>{
  //handle response, which has the format (Object){columnName:'type'}
 })
 .catch((error)=>{
  //handle error, which has the format (Object){code:http_status_code,description='http_status_description'}
 });
```
