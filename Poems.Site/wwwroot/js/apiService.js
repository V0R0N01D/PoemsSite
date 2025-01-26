import {ResultObject} from "./resultObject.js";

export class ApiService {
    constructor() {
        this.baseURL = '/api/'
    }

    async searchPoemsInDb(query) {
        const url = `${this.baseURL}poems?query=${encodeURIComponent(query)}`;
        return this.sendRequest(url);
    }

    async getPoemById(poemId) {
        const url = `${this.baseURL}poems/${poemId}`;
        return await this.sendRequest(url);
    }
    
    async sendRequest(url, method = 'GET', body) {
        try {
            const response = await fetch(url, body);
            return await this.processResponse(response);
        } catch (error) {
            let message = "Неизвестная ошибка.";
            if (error instanceof TypeError) {
                message = "Ошибка сети."
            }

            console.error(`Error sending a request to the api, details:`, error);
            return ResultObject.Fail(message);
        }
    }
    
    async processResponse(response) {
        if (!response.ok) {
            return ResultObject.Fail(await response.text());
        }

        return await this.deserializeResponse(response);
    }

    async deserializeResponse(response) {
        try {
            const responseData = await response.json();
            return ResultObject.Ok(responseData);
        } catch (error) {
            console.error("Deserialization error: ", error);
            return ResultObject.Fail("Ошибка десериализации ответа сервера.");
        }
    }


}