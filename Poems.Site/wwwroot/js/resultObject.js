export class ResultObject {
    constructor(success, result, error) {
        this.success = success;
        this.result = result;
        this.error = error;
    }

    static Ok(data) {
        return new ResultObject(true, data, null);
    }

    static Fail(errorMsg) {
        return new ResultObject(false, null, errorMsg);
    }
}